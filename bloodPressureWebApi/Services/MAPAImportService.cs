using bloodPressureWebApi.Data;
using bloodPressureWebApi.DTOs.Import;
using bloodPressureWebApi.Models;

using Microsoft.EntityFrameworkCore;

using OfficeOpenXml;

using System.Security.Cryptography;
using System.Text;

namespace bloodPressureWebApi.Services
{
    public class MAPAImportService
    {
        private readonly
            ApplicationDbContext _context;


        private const int
            ExperimentalReadingLimit =
                47;


        public MAPAImportService(
            ApplicationDbContext context)
        {
            _context =
                context;
        }


        // =====================================================
        // IMPORT STUDY FILE
        // =====================================================

        public async Task<PressureLoadDto>
            ImportStudyFile(
                IFormFile file,
                int studyId)
        {
            if (
                file == null
                ||
                file.Length == 0)
            {
                throw new Exception(
                    "Invalid file");
            }


            var study =
                await _context
                    .MAPAStudies
                    .FirstOrDefaultAsync(
                        x =>
                            x.StudyId ==
                            studyId);


            if (study == null)
            {
                throw new Exception(
                    "Study not found");
            }


            ExcelPackage.License
                .SetNonCommercialPersonal(
                    "Ivette");


            // =================================================
            // SAVE ORIGINAL FILE
            // =================================================

            string uploadsFolder =
                Path.Combine(
                    Directory
                        .GetCurrentDirectory(),

                    "Uploads");


            if (
                !Directory.Exists(
                    uploadsFolder))
            {
                Directory.CreateDirectory(
                    uploadsFolder);
            }


            string filePath =
                Path.Combine(
                    uploadsFolder,

                    Guid.NewGuid()
                    +
                    Path.GetExtension(
                        file.FileName));


            using (
                var stream =
                    new FileStream(
                        filePath,
                        FileMode.Create))
            {
                await file
                    .CopyToAsync(
                        stream);
            }


            // =================================================
            // OPEN EXCEL
            // =================================================

            using var package =
                new ExcelPackage(
                    new FileInfo(
                        filePath));


            var ws =
                package
                    .Workbook
                    .Worksheets["Modo MAPA"];


            if (ws == null)
            {
                ws =
                    package
                        .Workbook
                        .Worksheets["ABPM Mode"];
            }


            if (ws == null)
            {
                ws =
                    package
                        .Workbook
                        .Worksheets
                        .FirstOrDefault();
            }


            if (ws == null)
            {
                throw new Exception(
                    "Worksheet not found");
            }


            // =================================================
            // DAY / NIGHT SCHEDULE
            // =================================================

            TimeSpan awake =
                study.AwakeTime
                ??
                new TimeSpan(
                    6,
                    0,
                    0);


            TimeSpan sleep =
                study.SleepTime
                ??
                new TimeSpan(
                    22,
                    0,
                    0);


            // =================================================
            // PRESSURE LOAD
            //
            // Confirmed original locations.
            // =================================================

            string dayLoadText =
                ws.Cells["C11"]
                    .Text;


            string nightLoadText =
                ws.Cells["C12"]
                    .Text;


            string ageText =
                ws.Cells["F3"]
                    .Text
                    .Trim();


            dayLoadText =
                dayLoadText
                    .Replace(
                        "%",
                        "")
                    .Trim();


            nightLoadText =
                nightLoadText
                    .Replace(
                        "%",
                        "")
                    .Trim();


            double dayPressureLoad =
                double.TryParse(
                    dayLoadText,
                    out double dLoad)

                    ? dLoad
                    : 0;


            double nightPressureLoad =
                double.TryParse(
                    nightLoadText,
                    out double nLoad)

                    ? nLoad
                    : 0;


            // CORRECCION E1-B: igual que MATLAB (xlsread hoja 3, F3 -> si no
            // hay numero, 40). Se lee el VALOR de la celda, no el texto.
            object? ageValue = ws.Cells["F3"].Value;
            int excelAge = 40;
            try
            {
                if (ageValue != null &&
                    !(ageValue is string ageStr && string.IsNullOrWhiteSpace(ageStr)))
                {
                    excelAge = (int)Math.Round(
                        Convert.ToDouble(ageValue,
                            System.Globalization.CultureInfo.InvariantCulture),
                        MidpointRounding.AwayFromZero);
                }
            }
            catch
            {
                excelAge = 40;
            }

            study.Age = excelAge;


            // =================================================
            // READ MAPA MEASUREMENTS
            //
            // ORIGINAL FORMAT:
            //
            // row 28+
            //
            // B = Date
            // C = Time
            // D = Systolic
            // E = Diastolic
            // F = Pulse
            //
            // IMPORTANT:
            //
            // G23:G25 ARE NOT USED HERE.
            //
            // E1 takes the first 47 actual measurement rows.
            // =================================================

            // =================================================
            // CORRECCION E1-B
            //
            // Antes: se recorrian las filas 28..200, se SALTABAN en
            // silencio las filas cuyo texto de fecha/hora no se podia
            // convertir (DateTime.TryParse depende de la cultura del
            // equipo) y se seguia leyendo hasta juntar 47. Resultado:
            // en algunos estudios se perdian las primeras lecturas y
            // entraban filas posteriores a la 74.
            //
            // Ahora: igual que MATLAB (xlsread C28:C74 ... F28:F74),
            // se leen EXACTAMENTE las filas 28..74, por su VALOR
            // numerico (no por .Text), y si falta un dato se lanza
            // error en lugar de saltar la fila.
            // =================================================

            const int FirstRow = 28;
            const int LastRow = FirstRow + ExperimentalReadingLimit - 1; // 74

            int wakeSeconds = (int)Math.Round(awake.TotalSeconds);
            int sleepSeconds = (int)Math.Round(sleep.TotalSeconds);

            List<MAPAReading> readings =
                new();

            DateTime? currentDate = null;
            double previousTimeFraction = -1;

            for (int row = FirstRow; row <= LastRow; row++)
            {
                double timeFraction = ReadTimeFraction(ws.Cells[row, 3].Value, row);
                int systolic = ReadInteger(ws.Cells[row, 4].Value, row, "SYS (D)");
                int diastolic = ReadInteger(ws.Cells[row, 5].Value, row, "DIA (E)");
                int pulse = ReadInteger(ws.Cells[row, 6].Value, row, "PULSE (F)");

                // La FECHA solo se usa para guardar ReadingDateTime.
                // MATLAB no la usa. Si la celda B viene vacia o no se puede
                // leer, se arrastra la fecha anterior y se avanza un dia al
                // cruzar la medianoche.
                DateTime? dateFromCell = ReadDate(ws.Cells[row, 2].Value);

                if (dateFromCell.HasValue)
                {
                    currentDate = dateFromCell.Value.Date;
                }
                else if (currentDate.HasValue && timeFraction < previousTimeFraction)
                {
                    currentDate = currentDate.Value.AddDays(1);
                }

                currentDate ??= DateTime.Today.Date;
                previousTimeFraction = timeFraction;

                int secondsOfDay = (int)Math.Round(timeFraction * 86400.0);

                DateTime readingDate =
                    currentDate.Value.AddSeconds(secondsOfDay);

                // MATLAB: (tiempo >= hday) && (tiempo <= hnight)
                // AMBOS EXTREMOS INCLUIDOS: la lectura de 22:00 es DIA.
                bool isDay = wakeSeconds <= sleepSeconds
                    ? secondsOfDay >= wakeSeconds && secondsOfDay <= sleepSeconds
                    : secondsOfDay >= wakeSeconds || secondsOfDay <= sleepSeconds;

                readings.Add(
                    new MAPAReading
                    {
                        StudyId = studyId,
                        ReadingDateTime = readingDate,
                        Systolic = systolic,
                        Diastolic = diastolic,
                        Pulse = pulse,
                        Period = isDay ? "DAY" : "NIGHT"
                    });
            }


            // =================================================
            // VALIDATE IMPORT
            // =================================================

            if (
                !readings.Any())
            {
                throw new Exception(
                    "No readings detected");
            }


            // IMPORTANT:
            //
            // We do NOT compare against G23:G25.
            //
            // We simply report how many actual rows were found
            // up to the E1 maximum of 47.

            Console.WriteLine(
                $"[MAPA IMPORT] {file.FileName}: " +
                $"{readings.Count} measurements selected.");


            // =================================================
            // HASH BASED ON THE SAME MEASUREMENTS USED BY E1
            // =================================================

            study.ImportHash =
                GenerateHash(
                    readings);


            var existingStudy =
                await _context
                    .MAPAStudies
                    .FirstOrDefaultAsync(
                        x =>
                            x.ImportHash
                            ==
                            study.ImportHash
                            &&
                            x.StudyId
                            !=
                            studyId);


            if (
                existingStudy != null)
            {
                throw new Exception(
                    "Duplicate study detected");
            }


            // =================================================
            // SAVE READINGS
            // =================================================

            _context
                .MAPAReadings
                .AddRange(
                    readings);


            study.ExcelFileName =
                file.FileName;


            study.TotalReadings =
                readings.Count;


            // =================================================
            // IMPORTANT:
            //
            // Do NOT calculate:
            //
            // readings.Count / 47 * 100
            //
            // as "CorrectReadingsPercent".
            //
            // That is not a valid interpretation of clinical
            // valid/correct readings.
            // =================================================

            study.CorrectReadingsPercent =
                null;


            study.StudyStatus =
                "IMPORTED";


            await _context
                .SaveChangesAsync();


            return new PressureLoadDto
            {
                DayPressureLoad =
                    dayPressureLoad,

                NightPressureLoad =
                    nightPressureLoad
            };
        }


        // =====================================================
        // PREVIEW IMPORT
        // =====================================================

        public async Task<ImportPreviewDto>
            PreviewImport(
                IFormFile file,
                int patientId)
        {
            if (
                file == null
                ||
                file.Length == 0)
            {
                throw new Exception(
                    "Invalid file");
            }


            ExcelPackage.License
                .SetNonCommercialPersonal(
                    "Ivette");


            using var stream =
                new MemoryStream();


            await file
                .CopyToAsync(
                    stream);


            stream.Position =
                0;


            using var package =
                new ExcelPackage(
                    stream);


            var ws =
                package
                    .Workbook
                    .Worksheets["Modo MAPA"];


            if (ws == null)
            {
                ws =
                    package
                        .Workbook
                        .Worksheets["ABPM Mode"];
            }


            if (ws == null)
            {
                ws =
                    package
                        .Workbook
                        .Worksheets
                        .FirstOrDefault();
            }


            if (ws == null)
            {
                throw new Exception(
                    "Worksheet not found");
            }


            List<string> rawLines =
                new();


            int totalReadings =
                0;


            DateTime? firstDate =
                null;


            for (
                int row = 28;
                row <= 200;
                row++)
            {
                if (
                    totalReadings
                    >=
                    ExperimentalReadingLimit)
                {
                    break;
                }


                string dateText =
                    ws.Cells[row, 2]
                        .Text
                        .Trim();


                string timeText =
                    ws.Cells[row, 3]
                        .Text
                        .Trim();


                string sysText =
                    ws.Cells[row, 4]
                        .Text
                        .Trim();


                string diaText =
                    ws.Cells[row, 5]
                        .Text
                        .Trim();


                string pulseText =
                    ws.Cells[row, 6]
                        .Text
                        .Trim();


                if (
                    string.IsNullOrWhiteSpace(
                        dateText)
                    ||
                    string.IsNullOrWhiteSpace(
                        timeText)
                    ||
                    string.IsNullOrWhiteSpace(
                        sysText)
                    ||
                    string.IsNullOrWhiteSpace(
                        diaText)
                    ||
                    string.IsNullOrWhiteSpace(
                        pulseText))
                {
                    continue;
                }


                string line =
                    $"{dateText}-" +
                    $"{timeText}-" +
                    $"{sysText}-" +
                    $"{diaText}-" +
                    $"{pulseText}";


                rawLines.Add(
                    line);


                totalReadings++;


                if (
                    firstDate == null)
                {
                    if (
                        DateTime.TryParse(
                            $"{dateText} {timeText}",
                            out DateTime dt))
                    {
                        firstDate =
                            dt;
                    }
                }
            }


            string rawData =
                string.Join(
                    "|",
                    rawLines);


            string hash;


            using (
                SHA256 sha =
                    SHA256.Create())
            {
                byte[] bytes =
                    sha.ComputeHash(
                        Encoding.UTF8
                            .GetBytes(
                                rawData));


                hash =
                    Convert.ToHexString(
                        bytes);
            }


            var existing =
                await _context
                    .MAPAStudies
                    .Where(
                        x =>
                            x.PatientId
                            ==
                            patientId)
                    .OrderByDescending(
                        x =>
                            x.CreatedAt)
                    .FirstOrDefaultAsync(
                        x =>
                            x.ImportHash
                            ==
                            hash);


            ImportPreviewDto result =
                new();


            result.IncomingReadings =
                totalReadings;


            if (
                existing != null)
            {
                result.StudyExists =
                    true;


                result.SameHash =
                    true;


                result.ExistingStudyId =
                    existing.StudyId;


                result.ExistingFileName =
                    existing.ExcelFileName;


                result.ExistingStudyDate =
                    existing.StudyDate;


                result.RecommendedAction =
                    "SKIP_DUPLICATE";
            }
            else
            {
                result.StudyExists =
                    false;


                result.SameHash =
                    false;


                result.RecommendedAction =
                    "CREATE_NEW";
            }


            return result;
        }


        // =====================================================
        // HELPERS DE LECTURA (CORRECCION E1-B)
        // Leen el VALOR de la celda, no el texto formateado,
        // para no depender de la cultura del equipo.
        // =====================================================

        private static double ReadTimeFraction(object? value, int row)
        {
            switch (value)
            {
                case double d:
                    return d - Math.Floor(d);
                case DateTime dt:
                    return dt.TimeOfDay.TotalDays;
                case TimeSpan ts:
                    return ts.TotalDays - Math.Floor(ts.TotalDays);
                case string s when TimeSpan.TryParse(
                        s.Trim(), System.Globalization.CultureInfo.InvariantCulture, out var t):
                    return t.TotalDays - Math.Floor(t.TotalDays);
                case null:
                    throw new InvalidOperationException(
                        $"Fila {row}: la hora (columna C) esta vacia.");
                default:
                    try
                    {
                        double d = Convert.ToDouble(value,
                            System.Globalization.CultureInfo.InvariantCulture);
                        return d - Math.Floor(d);
                    }
                    catch
                    {
                        throw new InvalidOperationException(
                            $"Fila {row}: hora no valida en columna C: '{value}'.");
                    }
            }
        }

        private static int ReadInteger(object? value, int row, string column)
        {
            if (value == null || (value is string s0 && string.IsNullOrWhiteSpace(s0)))
                throw new InvalidOperationException(
                    $"Fila {row}: {column} esta vacia.");

            double d;
            try
            {
                d = value is string s
                    ? double.Parse(s.Trim(), System.Globalization.CultureInfo.InvariantCulture)
                    : Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                throw new InvalidOperationException(
                    $"Fila {row}: valor no numerico en {column}: '{value}'.");
            }

            // Mismo comportamiento que MATLAB round (lejos del cero).
            return (int)Math.Round(d, MidpointRounding.AwayFromZero);
        }

        private static DateTime? ReadDate(object? value)
        {
            switch (value)
            {
                case DateTime dt:
                    return dt;
                case double d when d > 1:
                    return DateTime.FromOADate(Math.Floor(d));
                case string s when !string.IsNullOrWhiteSpace(s):
                    var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "dd/MM/yy", "yyyy-MM-dd" };
                    if (DateTime.TryParseExact(s.Trim(), formats,
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out var parsed))
                        return parsed;
                    return null;
                default:
                    return null;
            }
        }


        // =====================================================
        // GENERATE HASH
        // =====================================================

        private string GenerateHash(
            List<MAPAReading> readings)
        {
            string raw =
                string.Join(
                    "|",

                    readings.Select(
                        x =>
                            $"{x.ReadingDateTime:yyyyMMddHHmm}" +
                            $"{x.Systolic}" +
                            $"{x.Diastolic}" +
                            $"{x.Pulse}"));


            using SHA256 sha =
                SHA256.Create();


            byte[] bytes =
                sha.ComputeHash(
                    Encoding.UTF8
                        .GetBytes(
                            raw));


            return Convert.ToHexString(
                bytes);
        }
    }
}
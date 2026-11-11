using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace bloodPressureWebApi.Services
{
    public sealed class E1BBackendTrace
    {
        private readonly string _folder;
        private readonly ILogger<E1BBackendTrace> _logger;
        private readonly object _sync = new();
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

        public E1BBackendTrace(IConfiguration configuration, ILogger<E1BBackendTrace> logger)
        {
            _logger = logger;
            // CORRECCION E1-B: antes, sin configuracion, la traza caia en
            // bin\Debug\netX\ExperimentResults\Trace (AppContext.BaseDirectory),
            // NO en la carpeta donde MATLAB escribe su traza. Ahora el default
            // es la misma carpeta que usa el script de MATLAB.
            string? configured = configuration["E1B:TraceDirectory"];
            _folder = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(@"C:\MAPA_E1_TEST", "ExperimentResults", "Trace")
                : Environment.ExpandEnvironmentVariables(configured);

            try
            {
                Directory.CreateDirectory(_folder);
                _logger.LogWarning("E1-B TRACE ACTIVE. Directory: {TraceDirectory}", _folder);
                Append("CSHARP_TRACE_TEST.csv", new[] { "Timestamp", "Message", "BaseDirectory", "CurrentDirectory" },
                    new[] { new object?[] { DateTime.Now, "E1BBackendTrace initialized", AppContext.BaseDirectory, Environment.CurrentDirectory } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "E1-B TRACE COULD NOT CREATE FILES. Target directory: {TraceDirectory}", _folder);
                throw;
            }
        }

        public string TraceDirectory => _folder;

        /// <summary>
        /// NUEVO: permite apagar la traza durante experimentos de rendimiento
        /// o reproducibilidad (E7 ejecuta ExtractFeatures cientos de veces y
        /// la escritura a disco inflaria los tiempos). Por defecto encendida.
        /// </summary>
        public bool Enabled { get; set; } = true;

        public void WriteReadings<T>(string fileName, IEnumerable<T> readings,
            Func<T, DateTime> dateTime, Func<T, double> systolic,
            Func<T, double> diastolic, Func<T, double> pulse,
            Func<T, string?> periodFromDatabase, double wakeHour, double sleepHour)
        {
            var rows = new List<object?[]>();
            int i = 0;
            foreach (var r in readings)
            {
                i++;
                var dt = dateTime(r);
                double h = dt.TimeOfDay.TotalHours;
                bool isDay = IsDaytime(h, wakeHour, sleepHour);
                rows.Add(new object?[] { fileName, i, dt.ToString("O", Inv), h,
                    systolic(r), diastolic(r), pulse(r), periodFromDatabase(r) ?? "",
                    isDay, !isDay, wakeHour, sleepHour });
            }
            Append("CSHARP_Readings.csv", new[] { "FileName", "RowIndex", "DateTime", "DecimalHours",
                "SYS", "DIA", "Pulse", "PeriodFromDB", "UsedAsDayByClock", "UsedAsNightByClock",
                "WakeHour", "SleepHour" }, rows);
        }

        public void WriteFeatures(string fileName, int studyId, int totalReadings, int dayCount,
            int nightCount, double daySysMean, double dayDiaMean, double nightSysMean,
            double nightDiaMean, double csis, double cdia, object? featureEntity = null)
        {
            Append("CSHARP_Features.csv", new[] { "FileName", "StudyId", "TotalReadings", "DayCount",
                "NightCount", "DaySysMeanRaw", "DayDiaMeanRaw", "NightSysMeanRaw", "NightDiaMeanRaw",
                "DaySysMeanRounded", "DayDiaMeanRounded", "NightSysMeanRounded", "NightDiaMeanRounded",
                "Csis_Backend", "Cdia_Backend", "FeatureEntityJson" }, new[] { new object?[] {
                fileName, studyId, totalReadings, dayCount, nightCount, daySysMean, dayDiaMean,
                nightSysMean, nightDiaMean, Math.Round(daySysMean, MidpointRounding.AwayFromZero),
                Math.Round(dayDiaMean, MidpointRounding.AwayFromZero),
                Math.Round(nightSysMean, MidpointRounding.AwayFromZero),
                Math.Round(nightDiaMean, MidpointRounding.AwayFromZero), csis, cdia,
                featureEntity == null ? "" : JsonSerializer.Serialize(featureEntity, JsonOptions) } });
        }

        public void WriteModelStage(string fileName, int studyId, string stage, object? inputs,
            object? rawOutput, object? finalOutput, string? notes = null)
        {
            Append("CSHARP_ModelStages.csv", new[] { "FileName", "StudyId", "Stage", "InputsJson",
                "RawOutputJson", "FinalOutputJson", "Notes" }, new[] { new object?[] {
                fileName, studyId, stage, ToJson(inputs), ToJson(rawOutput), ToJson(finalOutput), notes ?? "" } });
        }

        public void WriteError(string fileName, int studyId, Exception ex)
        {
            Append("CSHARP_Errors.csv", new[] { "FileName", "StudyId", "ExceptionType", "Message", "Details" },
                new[] { new object?[] { fileName, studyId, ex.GetType().FullName, ex.Message, ex.ToString() } });
        }

        public static bool IsDaytime(double hour, double wakeHour, double sleepHour)
        {
            if (wakeHour < 0 || wakeHour >= 24 || sleepHour < 0 || sleepHour >= 24)
                throw new ArgumentOutOfRangeException(nameof(wakeHour), "WakeHour/SleepHour deben estar entre 0 y 23.999.");
            // MATLAB: 06:00 a 22:00, ambos extremos incluidos.
            return wakeHour <= sleepHour
                ? hour >= wakeHour && hour <= sleepHour
                : hour >= wakeHour || hour <= sleepHour;
        }

        private static string ToJson(object? o) => o == null ? "" : JsonSerializer.Serialize(o, JsonOptions);
        private void Append(string name, IReadOnlyList<string> headers, IEnumerable<object?[]> rows)
        {
            if (!Enabled) return;

            lock (_sync)
            {
                Directory.CreateDirectory(_folder);
                string path = Path.Combine(_folder, name);
                bool header = !File.Exists(path) || new FileInfo(path).Length == 0;
                using var w = new StreamWriter(path, true, new UTF8Encoding(true));
                if (header) w.WriteLine(string.Join(",", headers.Select(Csv)));
                foreach (var row in rows) w.WriteLine(string.Join(",", row.Select(v => Csv(Format(v)))));
                w.Flush();
                _logger.LogInformation("E1-B trace file written: {Path}", path);
            }
        }
        private static string Format(object? v) => v switch
        {
            null => "",
            DateTime d => d.ToString("O", Inv),
            double d => d.ToString("G17", Inv),
            float f => f.ToString("G9", Inv),
            decimal m => m.ToString(Inv),
            bool b => b ? "true" : "false",
            _ => Convert.ToString(v, Inv) ?? ""
        };
        private static string Csv(string? s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";

        // CORRECCION E1-B: antes escribia en AppContext.BaseDirectory (otra
        // carpeta) y con un encabezado distinto al del constructor, mezclando
        // formatos en el mismo CSV. Ahora usa la misma carpeta y su propio archivo.
        public void WriteDiagnostic(string message)
        {
            Append("CSHARP_Diagnostics.csv", new[] { "Timestamp", "Message" },
                new[] { new object?[] { DateTime.Now, message } });
        }

        // NUEVO: una fila por etapa y por estudio, con MATLAB y backend lado a
        // lado. Es la traza que permite ubicar en que etapa nace la diferencia
        // (ANN, BPLevel, HeartRate, medias dia/noche, ratios, Nocturnal).
        public void WriteStageComparison(Guid batchId, string fileName, int studyId,
            string stage, string variable, double matlab, double backend,
            double tolerance, string? notes = null)
        {
            double diff = backend - matlab;
            Append("CSHARP_StageComparison.csv", new[] { "BatchId", "FileName", "StudyId",
                "Stage", "Variable", "Matlab", "Backend", "Difference", "AbsDifference",
                "Tolerance", "Match", "Notes" }, new[] { new object?[] {
                batchId.ToString(), fileName, studyId, stage, variable, matlab, backend,
                diff, Math.Abs(diff), tolerance, Math.Abs(diff) <= tolerance, notes ?? "" } });
        }

        // NUEVO: cobertura del lote. Registra por que un archivo de MATLAB no
        // entro a la comparacion (sin estudio, sin features, duplicado, etc.).
        public void WriteCoverage(Guid batchId, string fileName, string status,
            int? studyId, int duplicatesFound, string? detail = null)
        {
            Append("CSHARP_Coverage.csv", new[] { "BatchId", "FileName", "Status",
                "StudyIdUsed", "StudiesWithSameFile", "Detail" }, new[] { new object?[] {
                batchId.ToString(), fileName, status, studyId, duplicatesFound, detail ?? "" } });
        }

        // NUEVO: borra las trazas C# de corridas anteriores (igual que hace MATLAB).
        public void ResetRunFiles()
        {
            lock (_sync)
            {
                foreach (var name in new[] { "CSHARP_StageComparison.csv",
                    "CSHARP_Coverage.csv", "CSHARP_Errors.csv" })
                {
                    string path = Path.Combine(_folder, name);
                    if (File.Exists(path)) File.Delete(path);
                }
            }
        }
    }


}
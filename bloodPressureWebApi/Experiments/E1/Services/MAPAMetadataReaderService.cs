using bloodPressureWebApi.DTOs.Experiments;

using OfficeOpenXml;

using System.Globalization;
using System.Text.RegularExpressions;

namespace bloodPressureWebApi.Services
{
    public class MAPAMetadataReaderService
    {
        public MAPAMetadataDto Read(
            string filePath)
        {
            // ================================================
            // VALIDATION
            // ================================================

            if (
                string.IsNullOrWhiteSpace(
                    filePath))
            {
                throw new ArgumentException(
                    "File path is required.",
                    nameof(filePath));
            }


            if (
                !File.Exists(
                    filePath))
            {
                throw new FileNotFoundException(
                    "MAPA Excel file not found.",
                    filePath);
            }


            // ================================================
            // OPEN EXCEL
            // ================================================

            using var package =
                new ExcelPackage(
                    new FileInfo(
                        filePath));


            ExcelWorksheet? ws =
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
                        .Worksheets["ABPM mode"];
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
                throw new InvalidOperationException(
                    $"No worksheet found in " +
                    $"'{Path.GetFileName(filePath)}'.");
            }


            // ================================================
            // METADATA
            // ================================================

            var metadata =
                new MAPAMetadataDto
                {
                    SourceFileName =
                        Path.GetFileName(
                            filePath),

                    ExternalPatientCode =
                        Path.GetFileNameWithoutExtension(
                            filePath)
                };


            // ================================================
            // 24-HOUR SUMMARY
            //
            // ROW 23
            //
            // D = SYS
            // E = DIA
            // F = PULSE
            // ================================================

            metadata.Original24hSystolicMean =
                ReadMeanCell(
                    ws.Cells["D94"]);


            metadata.Original24hDiastolicMean =
                ReadMeanCell(
                    ws.Cells["E94"]);


            metadata.OriginalMeanPulse =
                ReadMeanCell(
                    ws.Cells["F94"]);


            // ================================================
            // DAY / AWAKE SUMMARY
            //
            // ROW 24
            // ================================================

            metadata.OriginalDaySystolicMean =
                ReadMeanCell(
                    ws.Cells["D95"]);


            metadata.OriginalDayDiastolicMean =
                ReadMeanCell(
                    ws.Cells["E95"]);


            metadata.OriginalDayPulseMean =
                ReadMeanCell(
                    ws.Cells["F95"]);


            // ================================================
            // NIGHT / ASLEEP SUMMARY
            //
            // ROW 25
            // ================================================

            metadata.OriginalNightSystolicMean =
                ReadMeanCell(
                    ws.Cells["D96"]);


            metadata.OriginalNightDiastolicMean =
                ReadMeanCell(
                    ws.Cells["E96"]);


            metadata.OriginalNightPulseMean =
                ReadMeanCell(
                    ws.Cells["F96"]);


            // ================================================
            // PRESSURE LOAD
            //
            // CONFIRMED LEGACY MAPA FORMAT
            //
            // C11 = DAY
            // C12 = NIGHT
            // ================================================

            metadata.OriginalDayPressureLoad =
    NormalizePressureLoad(
        ReadPercentageCell(
            ws.Cells["C11"]));

            metadata.OriginalNightPressureLoad =
                NormalizePressureLoad(
                    ReadPercentageCell(
                        ws.Cells["C12"]));


            // ================================================
            // OPTIONAL DIPPING
            //
            // We do NOT invent coordinates here.
            // Leave null until explicitly validated.
            // ================================================

            metadata.OriginalSystolicDippingPercent =
                null;


            metadata.OriginalDiastolicDippingPercent =
                null;


            // ================================================
            // STRICT E1 VALIDATION
            //
            // E1 MUST NOT silently continue with missing
            // reference variables.
            // ================================================

            ValidateE1Reference(
                metadata);


            // ================================================
            // DIAGNOSTIC OUTPUT
            // ================================================

            Console.WriteLine(
                "==========================================");


            Console.WriteLine(
                $"[E1 REFERENCE] " +
                $"{metadata.SourceFileName}");


            Console.WriteLine(
                $"24h SYS   = " +
                $"{metadata.Original24hSystolicMean}");


            Console.WriteLine(
                $"24h DIA   = " +
                $"{metadata.Original24hDiastolicMean}");


            Console.WriteLine(
                $"24h Pulse = " +
                $"{metadata.OriginalMeanPulse}");


            Console.WriteLine(
                $"Day SYS   = " +
                $"{metadata.OriginalDaySystolicMean}");


            Console.WriteLine(
                $"Day DIA   = " +
                $"{metadata.OriginalDayDiastolicMean}");


            Console.WriteLine(
                $"Night SYS = " +
                $"{metadata.OriginalNightSystolicMean}");


            Console.WriteLine(
                $"Night DIA = " +
                $"{metadata.OriginalNightDiastolicMean}");


            Console.WriteLine(
                $"Day Load  = " +
                $"{metadata.OriginalDayPressureLoad}");


            Console.WriteLine(
                $"Night Load= " +
                $"{metadata.OriginalNightPressureLoad}");


            Console.WriteLine(
                "==========================================");


            return metadata;
        }


        // ====================================================
        // READ MEAN CELL
        //
        // Supports:
        //
        // numeric:
        // 68.59574468085107
        //
        // text:
        // "68 (10)"
        // "83 (12)"
        // "125.4"
        // ====================================================

        private static double?
            ReadMeanCell(
                ExcelRange cell)
        {
            if (
                cell.Value == null)
            {
                return null;
            }


            // ================================================
            // NUMERIC TYPES
            // ================================================

            if (
                cell.Value
                is double doubleValue)
            {
                return doubleValue;
            }


            if (
                cell.Value
                is decimal decimalValue)
            {
                return Convert.ToDouble(
                    decimalValue);
            }


            if (
                cell.Value
                is int intValue)
            {
                return intValue;
            }


            if (
                cell.Value
                is long longValue)
            {
                return longValue;
            }


            if (
                cell.Value
                is float floatValue)
            {
                return floatValue;
            }


            // ================================================
            // TEXT
            // ================================================

            string text =
                cell.Text?
                    .Trim()
                ??
                string.Empty;


            if (
                string.IsNullOrWhiteSpace(
                    text))
            {
                return null;
            }


            // Example:
            //
            // "68 (10)"
            //
            // Keep only:
            //
            // "68"

            int parenthesisIndex =
                text.IndexOf(
                    '(');


            if (
                parenthesisIndex > 0)
            {
                text =
                    text
                        .Substring(
                            0,
                            parenthesisIndex)
                        .Trim();
            }


            text =
                text.Replace(
                    ",",
                    ".");


            Match match =
                Regex.Match(
                    text,
                    @"[-+]?\d+(?:\.\d+)?");


            if (
                !match.Success)
            {
                return null;
            }


            if (
                double.TryParse(
                    match.Value,

                    NumberStyles.Any,

                    CultureInfo.InvariantCulture,

                    out double parsed))
            {
                return parsed;
            }


            return null;
        }


        // ====================================================
        // READ PERCENTAGE
        // ====================================================

        private static double?
            ReadPercentageCell(
                ExcelRange cell)
        {
            if (
                cell.Value == null)
            {
                return null;
            }


            if (
                cell.Value
                is double doubleValue)
            {
                return doubleValue;
            }


            if (
                cell.Value
                is decimal decimalValue)
            {
                return Convert.ToDouble(
                    decimalValue);
            }


            if (
                cell.Value
                is int intValue)
            {
                return intValue;
            }


            string text =
                cell.Text?
                    .Trim()
                ??
                string.Empty;


            if (
                string.IsNullOrWhiteSpace(
                    text))
            {
                return null;
            }


            text =
                text
                    .Replace(
                        "%",
                        "")
                    .Replace(
                        ",",
                        ".")
                    .Trim();


            Match match =
                Regex.Match(
                    text,
                    @"[-+]?\d+(?:\.\d+)?");


            if (
                !match.Success)
            {
                return null;
            }


            if (
                double.TryParse(
                    match.Value,

                    NumberStyles.Any,

                    CultureInfo.InvariantCulture,

                    out double parsed))
            {
                return parsed;
            }


            return null;
        }


        // ====================================================
        // STRICT REFERENCE VALIDATION
        // ====================================================

        private static void
            ValidateE1Reference(
                MAPAMetadataDto metadata)
        {
            List<string> missing =
                new();


            if (
                !metadata
                    .OriginalDaySystolicMean
                    .HasValue)
            {
                missing.Add(
                    "DaySystolicMean");
            }


            if (
                !metadata
                    .OriginalDayDiastolicMean
                    .HasValue)
            {
                missing.Add(
                    "DayDiastolicMean");
            }


            if (
                !metadata
                    .OriginalNightSystolicMean
                    .HasValue)
            {
                missing.Add(
                    "NightSystolicMean");
            }


            if (
                !metadata
                    .OriginalNightDiastolicMean
                    .HasValue)
            {
                missing.Add(
                    "NightDiastolicMean");
            }


            if (
                !metadata
                    .OriginalMeanPulse
                    .HasValue)
            {
                missing.Add(
                    "24hMeanPulse");
            }


            if (
                !metadata
                    .OriginalDayPressureLoad
                    .HasValue)
            {
                missing.Add(
                    "DayPressureLoad");
            }


            if (
                !metadata
                    .OriginalNightPressureLoad
                    .HasValue)
            {
                missing.Add(
                    "NightPressureLoad");
            }


            if (
                missing.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Incomplete E1 reference metadata " +
                    $"in '{metadata.SourceFileName}'. " +
                    $"Missing: " +
                    $"{string.Join(", ", missing)}");
            }
        }

        // ====================================================
        // NORMALIZE PRESSURE LOAD
        //
        // Excel percentage cells may be stored internally as:
        //
        // 0.5625  -> displayed as 56.25 %
        //
        // Backend representation:
        //
        // 56.25
        //
        // This method normalizes the reference value to
        // the 0-100 percentage scale used by the backend.
        // ====================================================

        private static double?
            NormalizePressureLoad(
                double? value)
        {
            if (
                !value.HasValue)
            {
                return null;
            }

            double result =
                value.Value;

            // Excel fractional percentage:
            //
            // 0.5625 -> 56.25
            //
            // Important:
            // 0 remains 0.

            if (
                result > 0
                &&
                result <= 1.0)
            {
                result *=
                    100.0;
            }

            return result;
        }
    }
}
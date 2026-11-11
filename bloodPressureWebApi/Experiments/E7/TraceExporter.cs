using CsvHelper;
using System.Formats.Asn1;
using System.Globalization;

namespace bloodPressureWebApi.Experiments.E7
{
    public static class TraceExporter
    {
        public static void Export(
            IEnumerable<TraceRecord> traces,
            string filePath)
        {
            using var writer = new StreamWriter(filePath);

            using var csv = new CsvWriter(
                writer,
                CultureInfo.InvariantCulture);

            csv.WriteRecords(traces);
        }
    }
}
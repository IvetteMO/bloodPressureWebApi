using bloodPressureWebApi.Data;
using bloodPressureWebApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace bloodPressureWebApi.Services
{
    /// <summary>
    /// CORRECCIONES E1-B (equivalencia con el script MATLAB legacy):
    ///  1. Orden de las lecturas = orden de FILA del Excel (ReadingId),
    ///     no ReadingDateTime. MATLAB usa C28:C74 en ese orden; si una
    ///     fecha se interpreta mal, ordenar por fecha cambia el vector
    ///     que entra a la ANN.
    ///  2. Dia/noche se toma de Period (calculado UNA sola vez en el
    ///     import con la regla de MATLAB: 06:00 y 22:00 incluidos).
    ///  3. Medias redondeadas como MATLAB: round(mean(x)) = AwayFromZero.
    ///  4. Csis/Cdia se calculan con las medias YA redondeadas
    ///     (MATLAB: csis = Snoche / Sdia).
    ///  5. Exige exactamente 47 lecturas y lecturas de dia y de noche,
    ///     igual que MATLAB (que lanza error en esos casos).
    /// </summary>
    public class FeatureExtractionService
    {
        private const int RequiredReadings = 47;

        private readonly ApplicationDbContext _context;
        private readonly MatlabService _matlab;
        private readonly E1BBackendTrace _trace;
        private readonly ILogger<FeatureExtractionService> _logger;
        private readonly double _wakeHour;
        private readonly double _sleepHour;

        public FeatureExtractionService(ApplicationDbContext context, MatlabService matlab,
            E1BBackendTrace trace, IConfiguration configuration, ILogger<FeatureExtractionService> logger)
        {
            _context = context;
            _matlab = matlab;
            _trace = trace;
            _logger = logger;
            _wakeHour = configuration.GetValue<double?>("E1B:WakeHour") ?? 6.0;
            _sleepHour = configuration.GetValue<double?>("E1B:SleepHour") ?? 22.0;
        }

        private static double RoundMatlab(double x) =>
            Math.Round(x, MidpointRounding.AwayFromZero);

        public async Task ExtractFeatures(int studyId, double dayPressureLoad, double nightPressureLoad)
        {
            string fileName = $"Study_{studyId}";

            try
            {
                // Nombre real del archivo para que la traza se pueda cruzar con MATLAB.
                var studyInfo = await _context.MAPAStudies.AsNoTracking()
                    .Where(x => x.StudyId == studyId)
                    .Select(x => new { x.ExcelFileName })
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrWhiteSpace(studyInfo?.ExcelFileName))
                    fileName = Path.GetFileName(studyInfo.ExcelFileName);

                // (1) ORDEN DE FILA, no de fecha.
                var readings = await _context.MAPAReadings
                    .Where(x => x.StudyId == studyId)
                    .OrderBy(x => x.ReadingId)
                    .ToListAsync();

                _trace.WriteReadings(fileName, readings,
                    x => x.ReadingDateTime, x => x.Systolic, x => x.Diastolic, x => x.Pulse,
                    x => x.Period, _wakeHour, _sleepHour);

                // (5) Igual que MATLAB: exactamente 47.
                if (readings.Count != RequiredReadings)
                    throw new InvalidOperationException(
                        $"Study {studyId}: se esperaban {RequiredReadings} lecturas y hay {readings.Count}.");

                // (2) Dia/noche desde el import (fuente unica de verdad).
                var day = readings.Where(x => x.Period == "DAY").ToList();
                var night = readings.Where(x => x.Period == "NIGHT").ToList();

                if (day.Count == 0 || night.Count == 0)
                    throw new InvalidOperationException(
                        $"Study {studyId}: no hay lecturas de dia o de noche (dia={day.Count}, noche={night.Count}).");

                double daySysRaw = day.Average(x => (double)x.Systolic);
                double dayDiaRaw = day.Average(x => (double)x.Diastolic);
                double nightSysRaw = night.Average(x => (double)x.Systolic);
                double nightDiaRaw = night.Average(x => (double)x.Diastolic);

                // (3) Redondeo MATLAB.
                double daySys = RoundMatlab(daySysRaw);
                double dayDia = RoundMatlab(dayDiaRaw);
                double nightSys = RoundMatlab(nightSysRaw);
                double nightDia = RoundMatlab(nightDiaRaw);

                // (4) Ratios con medias redondeadas.
                double sysRatio = nightSys / daySys;
                double diaRatio = nightDia / dayDia;

                // Descriptivas (NO entran a ningun FIS).
                double meanPulse = readings.Average(x => (double)x.Pulse);
                double dipping = (1.0 - sysRatio) * 100.0;

                _trace.WriteFeatures(fileName, studyId, readings.Count, day.Count, night.Count,
                    daySysRaw, dayDiaRaw, nightSysRaw, nightDiaRaw, sysRatio, diaRatio);

                // ANN: las 47 lecturas en orden de fila.
                int[] sysArray = readings.Select(x => x.Systolic).ToArray();
                int[] diaArray = readings.Select(x => x.Diastolic).ToArray();
                int[] pulseArray = readings.Select(x => x.Pulse).ToArray();

                double[] trends = _matlab.BPTrend(sysArray, diaArray, pulseArray);
                if (trends == null || trends.Length < 3)
                    throw new InvalidOperationException($"BPTrend returned fewer than 3 values for study {studyId}.");

                _trace.WriteModelStage(fileName, studyId, "BPTrend",
                    new { ReadingsUsed = readings.Count, Systolic = sysArray, Diastolic = diaArray, Pulse = pulseArray },
                    trends, trends, "Salida de MatlabService.BPTrend (comparar con MATLAB_ANN.csv)");

                var old = await _context.MAPAFeatures.FirstOrDefaultAsync(x => x.StudyId == studyId);
                if (old != null)
                {
                    _context.MAPAFeatures.Remove(old);
                    await _context.SaveChangesAsync();
                }

                var feature = new MAPAFeature
                {
                    StudyId = studyId,
                    DaySystolicMean = daySys,
                    DayDiastolicMean = dayDia,
                    NightSystolicMean = nightSys,
                    NightDiastolicMean = nightDia,
                    MeanPulse = meanPulse,
                    SystolicNightRatio = sysRatio,
                    DiastolicNightRatio = diaRatio,
                    DippingPercent = dipping,
                    DayPressureLoad = dayPressureLoad,
                    NightPressureLoad = nightPressureLoad,
                    TotalPressureLoad = (dayPressureLoad + nightPressureLoad) / 2,
                    SystolicTrend = trends[0],
                    DiastolicTrend = trends[1],
                    PulseTrend = trends[2],
                    // OJO: estas dos NO son metricas clinicas validas
                    // (lecturas/47 y 48 - lecturas). Ver nota en el LEEME.
                    ValidReadingsPercent = (readings.Count / 47.0) * 100,
                    ArtifactReadings = Math.Max(48 - readings.Count, 0),
                    CreatedAt = DateTime.Now
                };
                _context.MAPAFeatures.Add(feature);
                await _context.SaveChangesAsync();

                _logger.LogInformation("E1-B trace files are in {TraceDirectory}", _trace.TraceDirectory);
            }
            catch (Exception ex)
            {
                try { _trace.WriteError(fileName, studyId, ex); }
                catch (Exception traceEx) { _logger.LogError(traceEx, "Failed writing E1-B error trace."); }
                _logger.LogError(ex, "ExtractFeatures failed for study {StudyId}", studyId);
                throw;
            }
        }
    }
}

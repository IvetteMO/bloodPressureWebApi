using bloodPressureWebApi.DTOs.E1;
using bloodPressureWebApi.DTOs.Experiments;
using bloodPressureWebApi.Models;

namespace bloodPressureWebApi.Services
{
    /// <summary>
    /// Compares backend results against the MATLAB
    /// scientific reference for Experiment E1-B.
    /// </summary>
    public class E1BComparisonService
    {
        //====================================================
        // MAIN COMPARISON
        //====================================================

        public E1BValidationResult Compare(
            Guid batchId,
            int studyId,
            MAPAFeature feature,
            AIDiagnosis diagnosis,
            E1MatlabReferenceDto matlab)
        {
            var result =
                new E1BValidationResult
                {
                    BatchId = batchId,

                    StudyId = studyId,

                    FileName = matlab.FileName,

                    CreatedAt = DateTime.Now
                };

            //--------------------------------------------------
            // ANN
            //--------------------------------------------------

            result.MatlabSysANN =
                matlab.SysANN;

            result.BackendSysANN =
                feature.SystolicTrend ?? 0;

            result.SysANNAbsoluteError =
                AbsoluteError(
                    result.MatlabSysANN,
                    result.BackendSysANN);

            result.SysANNRelativeError =
                RelativeError(
                    result.MatlabSysANN,
                    result.BackendSysANN);

            result.SysANNMatch =
                Match(
                    result.SysANNAbsoluteError);

            //--------------------------------------------------

            result.MatlabDiaANN =
                matlab.DiaANN;

            result.BackendDiaANN =
                feature.DiastolicTrend ?? 0;

            result.DiaANNAbsoluteError =
                AbsoluteError(
                    result.MatlabDiaANN,
                    result.BackendDiaANN);

            result.DiaANNRelativeError =
                RelativeError(
                    result.MatlabDiaANN,
                    result.BackendDiaANN);

            result.DiaANNMatch =
                Match(
                    result.DiaANNAbsoluteError);

            //--------------------------------------------------

            result.MatlabPulseANN =
                matlab.PulseANN;

            result.BackendPulseANN =
                feature.PulseTrend ?? 0;

            result.PulseANNAbsoluteError =
                AbsoluteError(
                    result.MatlabPulseANN,
                    result.BackendPulseANN);

            result.PulseANNRelativeError =
                RelativeError(
                    result.MatlabPulseANN,
                    result.BackendPulseANN);

            result.PulseANNMatch =
                Match(
                    result.PulseANNAbsoluteError);

            //--------------------------------------------------
            // FUZZY BP
            //--------------------------------------------------

            result.MatlabBPLevel =
                matlab.BPLevelOutput;

            result.BackendBPLevel =
                diagnosis.FuzzyBPRisk ?? 0;

            result.BPLevelAbsoluteError =
                AbsoluteError(
                    result.MatlabBPLevel,
                    result.BackendBPLevel);

            result.BPLevelRelativeError =
                RelativeError(
                    result.MatlabBPLevel,
                    result.BackendBPLevel);

            result.BPLevelMatch =
                Match(
                    result.BPLevelAbsoluteError);

            //--------------------------------------------------
            // HEART RATE
            //--------------------------------------------------

            result.MatlabHeartRate =
                matlab.HeartRateOutput;

            result.BackendHeartRate =
                diagnosis.FuzzyHeartRC ?? 0;

            result.HeartRateAbsoluteError =
                AbsoluteError(
                    result.MatlabHeartRate,
                    result.BackendHeartRate);

            result.HeartRateRelativeError =
                RelativeError(
                    result.MatlabHeartRate,
                    result.BackendHeartRate);

            result.HeartRateMatch =
                Match(
                    result.HeartRateAbsoluteError);

            //--------------------------------------------------
            // DAY MEAN
            //--------------------------------------------------

            result.MatlabDaySys =
                matlab.Sdia;

            result.BackendDaySys =
                feature.DaySystolicMean ?? 0;

            result.DaySysAbsoluteError =
                AbsoluteError(
                    result.MatlabDaySys,
                    result.BackendDaySys);

            result.DaySysRelativeError =
                RelativeError(
                    result.MatlabDaySys,
                    result.BackendDaySys);

            result.DaySysMatch =
                Match(
                    result.DaySysAbsoluteError);

            //--------------------------------------------------

            result.MatlabDayDia =
                matlab.Ddia;

            result.BackendDayDia =
                feature.DayDiastolicMean ?? 0;

            result.DayDiaAbsoluteError =
                AbsoluteError(
                    result.MatlabDayDia,
                    result.BackendDayDia);

            result.DayDiaRelativeError =
                RelativeError(
                    result.MatlabDayDia,
                    result.BackendDayDia);

            result.DayDiaMatch =
                Match(
                    result.DayDiaAbsoluteError);

            //--------------------------------------------------
            // NIGHT MEAN
            //--------------------------------------------------

            result.MatlabNightSys =
                matlab.Snoche;

            result.BackendNightSys =
                feature.NightSystolicMean ?? 0;

            result.NightSysAbsoluteError =
                AbsoluteError(
                    result.MatlabNightSys,
                    result.BackendNightSys);

            result.NightSysRelativeError =
                RelativeError(
                    result.MatlabNightSys,
                    result.BackendNightSys);

            result.NightSysMatch =
                Match(
                    result.NightSysAbsoluteError);

            //--------------------------------------------------

            result.MatlabNightDia =
                matlab.Dnoche;

            result.BackendNightDia =
                feature.NightDiastolicMean ?? 0;

            result.NightDiaAbsoluteError =
                AbsoluteError(
                    result.MatlabNightDia,
                    result.BackendNightDia);

            result.NightDiaRelativeError =
                RelativeError(
                    result.MatlabNightDia,
                    result.BackendNightDia);

            result.NightDiaMatch =
                Match(
                    result.NightDiaAbsoluteError);

            //--------------------------------------------------
            // PRESSURE LOAD
            //--------------------------------------------------

            //result.MatlabDayLoad =
            //    matlab.Csis;

            //result.BackendDayLoad =
            //    feature.DayPressureLoad ?? 0;

            //result.DayLoadAbsoluteError =
            //    AbsoluteError(
            //        result.MatlabDayLoad,
            //        result.BackendDayLoad);

            //result.DayLoadRelativeError =
            //    RelativeError(
            //        result.MatlabDayLoad,
            //        result.BackendDayLoad);

            //result.DayLoadMatch =
            //    Match(
            //        result.DayLoadAbsoluteError);

            //--------------------------------------------------

            //result.MatlabNightLoad =
            //    matlab.Cdia;

            //result.BackendNightLoad =
            //    feature.NightPressureLoad ?? 0;

            //result.NightLoadAbsoluteError =
            //    AbsoluteError(
            //        result.MatlabNightLoad,
            //        result.BackendNightLoad);

            //result.NightLoadRelativeError =
            //    RelativeError(
            //        result.MatlabNightLoad,
            //        result.BackendNightLoad);

            //result.NightLoadMatch =
            //    Match(
            //result.NightLoadAbsoluteError);

            //====================================================
            // NOCTURNAL RATIOS: Csis y Cdia
            //====================================================

            result.MatlabCsis = matlab.Csis;
            result.BackendCsis =
                feature.SystolicNightRatio ?? 0;

            result.CsisAbsoluteError =
                AbsoluteError(
                    result.MatlabCsis,
                    result.BackendCsis);

            result.CsisRelativeError =
                RelativeError(
                    result.MatlabCsis,
                    result.BackendCsis);

            result.CsisMatch =
                Match(result.CsisAbsoluteError);

            //----------------------------------------------------

            result.MatlabCdia = matlab.Cdia;
            result.BackendCdia =
                feature.DiastolicNightRatio ?? 0;

            result.CdiaAbsoluteError =
                AbsoluteError(
                    result.MatlabCdia,
                    result.BackendCdia);

            result.CdiaRelativeError =
                RelativeError(
                    result.MatlabCdia,
                    result.BackendCdia);

            result.CdiaMatch =
                Match(result.CdiaAbsoluteError);

            // CORRECCION E1-B: aqui se sumaban Csis/Cdia, pero mas abajo
            // PassedComparisons se reinicia a 0 y se perdian. El conteo se
            // movio al bloque GLOBAL SUMMARY.


            //--------------------------------------------------
            // NOCTURNAL FUZZY
            //--------------------------------------------------

            result.MatlabNocturnalOutput =
                matlab.NocturnalOutput;

            result.BackendNocturnalOutput =
                diagnosis.FuzzyNocturnalC ?? 0;

            result.NocturnalAbsoluteError =
                AbsoluteError(
                    result.MatlabNocturnalOutput,
                    result.BackendNocturnalOutput);

            result.NocturnalRelativeError =
                RelativeError(
                    result.MatlabNocturnalOutput,
                    result.BackendNocturnalOutput);

            result.NocturnalMatch =
                Match(
                    result.NocturnalAbsoluteError);

            //--------------------------------------------------
            // PROFILE
            //--------------------------------------------------

            result.MatlabProfile =
                matlab.NocturnalProfile;

            result.BackendProfile =
                diagnosis.NocturnalProfile ?? "";

            result.ProfileMatch =
                string.Equals(
                    result.MatlabProfile.Trim(),
                    result.BackendProfile.Trim(),
                    StringComparison.OrdinalIgnoreCase);

            //--------------------------------------------------
            // GLOBAL SUMMARY
            //--------------------------------------------------

            result.TotalComparisons = 13;

            result.PassedComparisons = 0;

            if (result.SysANNMatch)
                result.PassedComparisons++;

            if (result.DiaANNMatch)
                result.PassedComparisons++;

            if (result.PulseANNMatch)
                result.PassedComparisons++;

            if (result.BPLevelMatch)
                result.PassedComparisons++;

            if (result.HeartRateMatch)
                result.PassedComparisons++;

            if (result.DaySysMatch)
                result.PassedComparisons++;

            if (result.DayDiaMatch)
                result.PassedComparisons++;

            if (result.NightSysMatch)
                result.PassedComparisons++;

            if (result.NightDiaMatch)
                result.PassedComparisons++;

            //if (result.DayLoadMatch)
            //    result.PassedComparisons++;

            //if (result.NightLoadMatch)
            //    result.PassedComparisons++;

            // CORRECCION E1-B: Csis y Cdia ahora si cuentan (13 de 13).
            if (result.CsisMatch)
                result.PassedComparisons++;

            if (result.CdiaMatch)
                result.PassedComparisons++;

            if (result.NocturnalMatch)
                result.PassedComparisons++;

            if (result.ProfileMatch)
                result.PassedComparisons++;

            result.FailedComparisons =
                result.TotalComparisons -
                result.PassedComparisons;

            result.AgreementPercentage =
                (double)result.PassedComparisons /
                result.TotalComparisons * 100.0;

            result.ValidationPassed =
                result.AgreementPercentage >= 95.0;

            result.Notes =
                result.ValidationPassed
                ? "Validation passed."
                : "Differences detected.";

            return result;
        }

        //====================================================
        // ABSOLUTE ERROR
        //====================================================

        private static double AbsoluteError(
            double matlab,
            double backend)
        {
            return Math.Abs(
                backend - matlab);
        }

        //====================================================
        // RELATIVE ERROR (%)
        //====================================================

        private static double RelativeError(
            double matlab,
            double backend)
        {
            if (Math.Abs(matlab) < 1e-12)
            {
                return 0.0;
            }

            return
                Math.Abs(
                    backend - matlab)

                /

                Math.Abs(matlab)

                * 100.0;
        }

        //====================================================
        // MATCH
        //====================================================

        /// <summary>
        /// Determines whether two numerical values
        /// are considered equivalent.
        /// </summary>
        private static bool Match(
            double absoluteError,
            double tolerance = 1e-6)
        {
            return absoluteError <= tolerance;
        }

        //====================================================
        // STRING MATCH
        //====================================================

        private static bool Match(
            string? matlab,
            string? backend)
        {
            matlab ??= string.Empty;
            backend ??= string.Empty;

            return string.Equals(
                matlab.Trim(),
                backend.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        //====================================================
        // AGREEMENT
        //====================================================

        private static double CalculateAgreement(
            int passed,
            int total)
        {
            if (total <= 0)
                return 0;

            return
                (double)passed
                /
                total
                * 100.0;
        }

        //====================================================
        // PASS / FAIL
        //====================================================

        private static bool ValidationPassed(
            double agreement,
            double threshold = 95.0)
        {
            return agreement >= threshold;
        }
    }
}


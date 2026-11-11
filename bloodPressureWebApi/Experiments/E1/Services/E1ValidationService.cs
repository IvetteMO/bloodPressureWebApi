using bloodPressureWebApi.Data;
using bloodPressureWebApi.DTOs.Experiments;
using bloodPressureWebApi.Models;

using Microsoft.EntityFrameworkCore;

namespace bloodPressureWebApi.Services
{
    public class E1ValidationService
    {
        private readonly
            ApplicationDbContext _context;


        public E1ValidationService(
            ApplicationDbContext context)
        {
            _context =
                context;
        }


        public async Task<MAPAValidationResult>
            ValidateStudy(
                Guid batchId,
                int studyId,
                MAPAMetadataDto reference)
        {
            // ================================================
            // STUDY
            // ================================================

            var study =
                await _context
                    .MAPAStudies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x =>
                            x.StudyId
                            ==
                            studyId);


            if (
                study == null)
            {
                throw new InvalidOperationException(
                    $"Study {studyId} not found.");
            }


            // ================================================
            // BACKEND FEATURES
            // ================================================

            var features =
                await _context
                    .MAPAFeatures
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x =>
                            x.StudyId
                            ==
                            studyId);


            if (
                features == null)
            {
                throw new InvalidOperationException(
                    $"MAPAFeatures not found " +
                    $"for StudyId={studyId}.");
            }


            // ================================================
            // RESULT
            // ================================================

            var result =
                new MAPAValidationResult
                {
                    BatchId =
                        batchId,

                    ExperimentCode =
                        "E1-A",

                    StudyId =
                        studyId,

                    ExternalPatientCode =
                        reference
                            .ExternalPatientCode,

                    SourceFileName =
                        reference
                            .SourceFileName,


                    // ========================================
                    // VALID READINGS
                    //
                    // Deliberately excluded from E1.
                    // ========================================

                    OriginalValidReadings =
                        null,

                    BackendValidReadings =
                        null,

                    ValidReadingsAbsoluteError =
                        null,


                    // ========================================
                    // DAY SYS
                    // ========================================

                    OriginalDaySystolicMean =
                        reference
                            .OriginalDaySystolicMean,

                    BackendDaySystolicMean =
                        features
                            .DaySystolicMean,

                    DaySystolicAbsoluteError =
                        AbsoluteError(
                            reference
                                .OriginalDaySystolicMean,

                            features
                                .DaySystolicMean),

                    DaySystolicRelativeError =
                        RelativeError(
                            reference
                                .OriginalDaySystolicMean,

                            features
                                .DaySystolicMean),


                    // ========================================
                    // DAY DIA
                    // ========================================

                    OriginalDayDiastolicMean =
                        reference
                            .OriginalDayDiastolicMean,

                    BackendDayDiastolicMean =
                        features
                            .DayDiastolicMean,

                    DayDiastolicAbsoluteError =
                        AbsoluteError(
                            reference
                                .OriginalDayDiastolicMean,

                            features
                                .DayDiastolicMean),

                    DayDiastolicRelativeError =
                        RelativeError(
                            reference
                                .OriginalDayDiastolicMean,

                            features
                                .DayDiastolicMean),


                    // ========================================
                    // NIGHT SYS
                    // ========================================

                    OriginalNightSystolicMean =
                        reference
                            .OriginalNightSystolicMean,

                    BackendNightSystolicMean =
                        features
                            .NightSystolicMean,

                    NightSystolicAbsoluteError =
                        AbsoluteError(
                            reference
                                .OriginalNightSystolicMean,

                            features
                                .NightSystolicMean),

                    NightSystolicRelativeError =
                        RelativeError(
                            reference
                                .OriginalNightSystolicMean,

                            features
                                .NightSystolicMean),


                    // ========================================
                    // NIGHT DIA
                    // ========================================

                    OriginalNightDiastolicMean =
                        reference
                            .OriginalNightDiastolicMean,

                    BackendNightDiastolicMean =
                        features
                            .NightDiastolicMean,

                    NightDiastolicAbsoluteError =
                        AbsoluteError(
                            reference
                                .OriginalNightDiastolicMean,

                            features
                                .NightDiastolicMean),

                    NightDiastolicRelativeError =
                        RelativeError(
                            reference
                                .OriginalNightDiastolicMean,

                            features
                                .NightDiastolicMean),


                    // ========================================
                    // 24H PULSE
                    // ========================================

                    OriginalMeanPulse =
                        reference
                            .OriginalMeanPulse,

                    BackendMeanPulse =
                        features
                            .MeanPulse,

                    MeanPulseAbsoluteError =
                        AbsoluteError(
                            reference
                                .OriginalMeanPulse,

                            features
                                .MeanPulse),

                    MeanPulseRelativeError =
                        RelativeError(
                            reference
                                .OriginalMeanPulse,

                            features
                                .MeanPulse),


                    // ========================================
                    // PRESSURE LOAD
                    //
                    // Integrity validation.
                    // ========================================

                    OriginalDayPressureLoad =
                        reference
                            .OriginalDayPressureLoad,

                    BackendDayPressureLoad =
                        features
                            .DayPressureLoad,

                    DayPressureLoadAbsoluteError =
                        AbsoluteError(
                            reference
                                .OriginalDayPressureLoad,

                            features
                                .DayPressureLoad),


                    OriginalNightPressureLoad =
                        reference
                            .OriginalNightPressureLoad,

                    BackendNightPressureLoad =
                        features
                            .NightPressureLoad,

                    NightPressureLoadAbsoluteError =
                        AbsoluteError(
                            reference
                                .OriginalNightPressureLoad,

                            features
                                .NightPressureLoad),


                    // ========================================
                    // DIPPING
                    //
                    // Not included in E1 until a validated
                    // reference source is defined.
                    // ========================================

                    OriginalSystolicDippingPercent =
                        null,

                    BackendSystolicDippingPercent =
                        null,

                    SystolicDippingAbsoluteError =
                        null,


                    OriginalDiastolicDippingPercent =
                        null,

                    BackendDiastolicDippingPercent =
                        null,

                    DiastolicDippingAbsoluteError =
                        null,


                    CreatedAt =
                        DateTime.Now
                };


            _context
                .MAPAValidationResults
                .Add(
                    result);


            await _context
                .SaveChangesAsync();


            return result;
        }


        // ====================================================
        // ABSOLUTE ERROR
        // ====================================================

        private static double?
            AbsoluteError(
                double? reference,
                double? backend)
        {
            if (
                !reference.HasValue
                ||
                !backend.HasValue)
            {
                return null;
            }


            return Math.Abs(
                backend.Value
                -
                reference.Value);
        }


        // ====================================================
        // RELATIVE ERROR %
        // ====================================================

        private static double?
            RelativeError(
                double? reference,
                double? backend)
        {
            if (
                !reference.HasValue
                ||
                !backend.HasValue)
            {
                return null;
            }


            if (
                Math.Abs(
                    reference.Value)
                <
                double.Epsilon)
            {
                return null;
            }


            return

                Math.Abs(
                    backend.Value
                    -
                    reference.Value)

                /

                Math.Abs(
                    reference.Value)

                *
                100.0;
        }
    }
}
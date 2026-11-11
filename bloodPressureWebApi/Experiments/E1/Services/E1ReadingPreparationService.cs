using bloodPressureWebApi.DTOs;

namespace bloodPressureWebApi.Services
{
    public class E1ReadingPreparationService
    {
        public const int RequiredReadings =
            47;


        // =====================================================
        // PREPARE EXACTLY 47 OBSERVATIONS
        // =====================================================

        public List<E1ReadingDto> Prepare(
            List<E1ReadingDto> sourceReadings)
        {
            if (
                sourceReadings == null
                ||
                sourceReadings.Count == 0)
            {
                throw new InvalidOperationException(
                    "No measurement rows were found " +
                    "in the MAPA columns.");
            }


            // =================================================
            // COPY SOURCE
            // =================================================

            var readings =
                sourceReadings
                    .Select(Clone)
                    .ToList();


            // =================================================
            // MORE THAN 47
            // =================================================

            if (
                readings.Count >
                RequiredReadings)
            {
                return readings
                    .Take(
                        RequiredReadings)
                    .ToList();
            }


            // =================================================
            // EXACTLY 47
            // =================================================

            if (
                readings.Count ==
                RequiredReadings)
            {
                return readings;
            }


            // =================================================
            // LESS THAN 47
            //
            // Compatibility strategy:
            // repeat existing observations until N=47.
            //
            // IMPORTANT:
            // This method is the ONLY place where the historical
            // completion rule should live.
            // =================================================

            return CompleteTo47(
                readings);
        }


        // =====================================================
        // HISTORICAL FIXED-LENGTH COMPLETION
        // =====================================================

        private List<E1ReadingDto> CompleteTo47(
            List<E1ReadingDto> readings)
        {
            var result =
                readings
                    .Select(Clone)
                    .ToList();


            int sourceIndex =
                0;


            while (
                result.Count <
                RequiredReadings)
            {
                var source =
                    readings[
                        sourceIndex
                        %
                        readings.Count];


                var replicated =
                    Clone(source);


                replicated.IsReplicated =
                    true;

                replicated.ReplicatedFromRow =
                    source.SourceRow;


                result.Add(
                    replicated);


                sourceIndex++;
            }


            return result;
        }


        // =====================================================
        // CLONE
        // =====================================================

        private static E1ReadingDto Clone(
            E1ReadingDto source)
        {
            return new E1ReadingDto
            {
                SourceRow =
                    source.SourceRow,

                DateTime =
                    source.DateTime,

                Time =
                    source.Time,

                Systolic =
                    source.Systolic,

                Diastolic =
                    source.Diastolic,

                Pulse =
                    source.Pulse,

                IsReplicated =
                    source.IsReplicated,

                ReplicatedFromRow =
                    source.ReplicatedFromRow
            };
        }
    }
}
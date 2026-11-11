using bloodPressureWebApi.Experiments.Core;
using System.Diagnostics;

namespace bloodPressureWebApi.Models
{
    public class DiagnosisJob
    {
        public int StudyId { get; init; }

        /// <summary>
        /// true por defecto (produccion sin cambios). Los experimentos lo
        /// ponen en false para excluir Framingham.
        /// </summary>
        public bool IncludeFramingham { get; init; } = true;

        public DateTime EnqueuedAt { get; set; }

        public DateTime? WorkerStartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public int RetryCount { get; set; }

        // CAMBIO: puede ser null (codigo de produccion que encola sin esperar).
        public TaskCompletionSource<bool>? CompletionSource { get; init; }

        // CAMBIO: opcional. Los experimentos nuevos miden con las marcas de
        // tiempo de abajo y no comparten un profiler entre hilos.
        public ExperimentProfiler? Profiler { get; init; }

        // =====================================================
        // NUEVO: marcas de tiempo de alta resolucion (Stopwatch ticks)
        // =====================================================

        /// <summary>Primera vez que se encolo (no cambia con reintentos).</summary>
        public long FirstEnqueuedTimestamp { get; set; }

        /// <summary>Ultimo intento: el worker toma el job.</summary>
        public long WorkerStartedTimestamp { get; set; }

        /// <summary>Ultimo intento: inicio y fin de RunDiagnosis.</summary>
        public long DiagnosisStartedTimestamp { get; set; }

        public long DiagnosisCompletedTimestamp { get; set; }

        /// <summary>Job terminado (exito o fallo definitivo).</summary>
        public long CompletedTimestamp { get; set; }

        /// <summary>Indice del consumidor que lo proceso (ultimo intento).</summary>
        public int ConsumerIndex { get; set; } = -1;

        public string? LastError { get; set; }

        public static double ElapsedMs(long from, long to)
            => from == 0 || to == 0
                ? double.NaN
                : (to - from) * 1000.0 / Stopwatch.Frequency;
    }
}

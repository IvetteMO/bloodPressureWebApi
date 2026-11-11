using bloodPressureWebApi.Models;
using System.Diagnostics;
using System.Threading.Channels;

namespace bloodPressureWebApi.Queues
{
    public class DiagnosisQueue
    {
        private readonly Channel<DiagnosisJob> _queue;

        public DiagnosisQueue()
        {
            _queue = Channel.CreateUnbounded<DiagnosisJob>();
        }

        // ==========================================
        // ENQUEUE
        // ==========================================

        public async Task EnqueueAsync(DiagnosisJob job)
        {
            job.EnqueuedAt = DateTime.UtcNow;

            // NUEVO: la primera marca no se sobrescribe en los reintentos,
            // para medir la latencia completa del job.
            if (job.FirstEnqueuedTimestamp == 0)
            {
                job.FirstEnqueuedTimestamp = Stopwatch.GetTimestamp();
            }

            await _queue.Writer.WriteAsync(job);
        }

        // ==========================================
        // DEQUEUE
        // ==========================================

        public async Task<DiagnosisJob> DequeueAsync(CancellationToken token)
        {
            return await _queue.Reader.ReadAsync(token);
        }

        /// <summary>
        /// NUEVO: espera a que haya un trabajo SIN tomarlo, para que el
        /// consumidor pueda revisar si sigue activo antes de consumir.
        /// </summary>
        public ValueTask<bool> WaitToReadAsync(CancellationToken token)
            => _queue.Reader.WaitToReadAsync(token);

        /// <summary>NUEVO: toma un trabajo si hay uno disponible.</summary>
        public bool TryDequeue(out DiagnosisJob job)
            => _queue.Reader.TryRead(out job!);

        /// <summary>NUEVO: trabajos pendientes en la cola.</summary>
        public int PendingCount => _queue.Reader.CanCount ? _queue.Reader.Count : -1;
    }
}

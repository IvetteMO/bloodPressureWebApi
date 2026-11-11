using System;
using System.Diagnostics;
using System.Threading;

namespace bloodPressureWebApi.Experiments.Core
{
    /// <summary>
    /// Measures the execution of a pipeline stage.
    /// Automatically records execution time, memory usage,
    /// thread information and execution status.
    /// </summary>
    public sealed class StageScope : IDisposable
    {
        private readonly ExperimentProfiler _profiler;
        private readonly Stopwatch _stopwatch;

        private readonly string _stageName;
        private readonly string? _parentStage;

        private readonly DateTime _startTime;

        private readonly long _managedMemoryBefore;
        private readonly long _workingSetBefore;
        private readonly long _privateMemoryBefore;

        private readonly int _managedThreadId;
        private readonly bool _isThreadPoolThread;

        private bool _disposed;
        private bool _success = true;
        private string? _exception;

        internal StageScope(
            ExperimentProfiler profiler,
            string stageName,
            string? parentStage)
        {
            _profiler = profiler;
            _stageName = stageName;
            _parentStage = parentStage;

            _startTime = DateTime.UtcNow;

            _managedMemoryBefore = GC.GetTotalMemory(false);

            var process = Process.GetCurrentProcess();

            _workingSetBefore = process.WorkingSet64;
            _privateMemoryBefore = process.PrivateMemorySize64;

            _managedThreadId = Environment.CurrentManagedThreadId;
            _isThreadPoolThread = Thread.CurrentThread.IsThreadPoolThread;

            _stopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// Marks the stage as failed.
        /// Call this inside a catch block before rethrowing.
        /// </summary>
        public void Fail(Exception exception)
        {
            _success = false;
            _exception = exception.ToString();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _stopwatch.Stop();

            var process = Process.GetCurrentProcess();

            long managedAfter = GC.GetTotalMemory(false);
            long workingAfter = process.WorkingSet64;
            long privateAfter = process.PrivateMemorySize64;

            var metric = new StageMetric
            {
                Experiment = _profiler.ExperimentName,
                Scenario = _profiler.Scenario,
                Iteration = _profiler.Iteration,

                Stage = _stageName,
                ParentStage = _parentStage,

                StartTime = _startTime,
                EndTime = DateTime.UtcNow,
                DurationMilliseconds = _stopwatch.Elapsed.TotalMilliseconds,

                ManagedMemoryBeforeBytes = _managedMemoryBefore,
                ManagedMemoryAfterBytes = managedAfter,

                WorkingSetBeforeBytes = _workingSetBefore,
                WorkingSetAfterBytes = workingAfter,

                PrivateMemoryBeforeBytes = _privateMemoryBefore,
                PrivateMemoryAfterBytes = privateAfter,

                ManagedThreadId = _managedThreadId,
                IsThreadPoolThread = _isThreadPoolThread,

                Success = _success,
                Exception = _exception
            };

            _profiler.Add(metric);
            _profiler.PopStage();
        }
    }
}
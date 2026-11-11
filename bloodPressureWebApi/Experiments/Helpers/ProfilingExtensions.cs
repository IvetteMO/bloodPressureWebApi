using bloodPressureWebApi.Experiments.Core;
using System;
using System.Threading.Tasks;

namespace bloodPressureWebApi.Experiments.Helpers
{
    /// <summary>
    /// Extension methods to profile synchronous and asynchronous operations
    /// without polluting production services with profiling code.
    /// </summary>
    public static class ProfilingExtensions
    {
        /// <summary>
        /// Measures a synchronous operation that does not return a value.
        /// </summary>
        public static void Measure(
            this ExperimentProfiler? profiler,
            string stageName,
            Action action)
        {
            if (profiler == null)
            {
                action();
                return;
            }

            using (profiler.Stage(stageName))
            {
                action();
            }
        }

        /// <summary>
        /// Measures a synchronous operation that returns a value.
        /// </summary>
        public static T Measure<T>(
            this ExperimentProfiler? profiler,
            string stageName,
            Func<T> func)
        {
            if (profiler == null)
            {
                return func();
            }

            using (profiler.Stage(stageName))
            {
                return func();
            }
        }

        /// <summary>
        /// Measures an asynchronous operation.
        /// </summary>
        public static async Task MeasureAsync(
            this ExperimentProfiler? profiler,
            string stageName,
            Func<Task> action)
        {
            if (profiler == null)
            {
                await action();
                return;
            }

            using (profiler.Stage(stageName))
            {
                await action();
            }
        }

        /// <summary>
        /// Measures an asynchronous operation that returns a value.
        /// </summary>
        public static async Task<T> MeasureAsync<T>(
            this ExperimentProfiler? profiler,
            string stageName,
            Func<Task<T>> func)
        {
            if (profiler == null)
            {
                return await func();
            }

            using (profiler.Stage(stageName))
            {
                return await func();
            }
        }
    }
}
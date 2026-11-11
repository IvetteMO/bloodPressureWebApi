using System.Collections.Concurrent;

namespace bloodPressureWebApi.Experiments.Core
{
    /// <summary>
    /// Collects metrics generated during an experiment.
    /// </summary>
    public sealed class ExperimentProfiler
    {
        private readonly ConcurrentBag<StageMetric> _metrics = new();
        private readonly ThreadLocal<Stack<string>> _stageStack = new(() => new Stack<string>());

        public string ExperimentName { get; }

        public string Scenario { get; }

        public int Iteration { get; }

        public bool ColdStart { get; }

        public bool IsAsync { get; }

        public int Concurrency { get; }

        public ExperimentProfiler(
            string experimentName,
            string scenario,
            int iteration,
            bool coldStart = false,
            bool isAsync = false,
            int concurrency = 1)
        {
            ExperimentName = experimentName;
            Scenario = scenario;
            Iteration = iteration;
            ColdStart = coldStart;
            IsAsync = isAsync;
            Concurrency = concurrency;
        }

        /// <summary>
        /// Starts a measurable execution stage.
        /// </summary>
        public StageScope Stage(string stageName)
        {
            var parentStage = CurrentStage;

            PushStage(stageName);

            return new StageScope(
                profiler: this,
                stageName: stageName,
                parentStage: parentStage);
        }

        internal void Add(StageMetric metric)
        {
            _metrics.Add(metric);
        }

        internal void PushStage(string stageName)
        {
            _stageStack.Value!.Push(stageName);
        }

        internal void PopStage()
        {
            Stack<string> stack = _stageStack.Value!;

            if (stack.Count > 0)
            {
                stack.Pop();
            }
        }

        internal string? CurrentStage
        {
            get
            {
                Stack<string> stack = _stageStack.Value!;

                return stack.Count == 0
                    ? null
                    : stack.Peek();
            }
        }

        public IReadOnlyCollection<StageMetric> Metrics =>
            _metrics.ToList().AsReadOnly();

        public void Clear()
        {
            while (_metrics.TryTake(out _))
            {
            }

            _stageStack.Value?.Clear();
        }

        public IReadOnlyList<StageStatistics> BuildStatistics()
        {
            return StatisticsService
                .CalculateAll(_metrics)
                .ToList();
        }

       
    }
}
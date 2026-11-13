namespace bloodPressureWebApi.Infrastructure.Matlab
{
    /// <summary>
    /// Estado de inicialización del MATLAB Runtime, compartido entre el
    /// servicio de calentamiento y el health check.
    /// </summary>
    public sealed class MatlabReadiness
    {
        private volatile bool _isReady;
        private volatile string? _error;

        public bool IsReady => _isReady;

        public string? Error => _error;

        public double? InitializationMs { get; private set; }

        public void MarkReady(double initializationMs)
        {
            InitializationMs = initializationMs;
            _error = null;
            _isReady = true;
        }

        public void MarkFailed(string error)
        {
            _error = error;
            _isReady = false;
        }
    }
}

using MathWorks.MATLAB.NET.Arrays;
using MySoftDiagnosisAssembly;

namespace bloodPressureWebApi.Services
{
    public class MatlabService : IDisposable
    {
        // ===================================================
        // FUZZY APIS
        // ===================================================

        private readonly BPLevelAPI _bpLevel;

        private readonly BPLoadAPI _bpLoad;

        private readonly NocturnalAPI _nocturnal;

        private readonly HeartRateAPI _heartRate;

        // ===================================================
        // NEURAL APIS
        // ===================================================

        private readonly BPTrendAPI _bpTrend;

        private readonly CVDRiskAPI _cvdRisk;

        private readonly FHSAPI _fhs;

        private readonly HRTrendAPI _hrTrend;

        private readonly SistoTrendAPI _sistoTrend;

        // ===================================================
        // CONSTRUCTOR
        // ===================================================

        // ===================================================
        // NUEVO: TIEMPO DE INICIALIZACION DEL RUNTIME
        //
        // Al crear la primera instancia de un componente compilado se
        // inicializa el MATLAB Runtime en el proceso. Ese costo ocurre AQUI
        // (constructor), antes de cualquier etapa medida por E2, asi que
        // hasta ahora no quedaba registrado en ningun experimento.
        // ===================================================

        /// <summary>Duracion del constructor de la primera instancia (ms).</summary>
        public static double? FirstInitializationMs { get; private set; }

        /// <summary>Duracion del constructor de esta instancia (ms).</summary>
        public double InitializationMs { get; }

        /// <summary>Momento (UTC) en que termino la inicializacion de esta instancia.</summary>
        public DateTime InitializedAtUtc { get; }

        private static int _instances;

        /// <summary>Instancias creadas en el proceso (1 si el servicio es Singleton).</summary>
        public static int InstancesCreated => _instances;

        public MatlabService()
        {
            var initTimer = System.Diagnostics.Stopwatch.StartNew();

            // ===============================================
            // FUZZY
            // ===============================================

            _bpLevel =
                new BPLevelAPI();

            _bpLoad =
                new BPLoadAPI();

            _nocturnal =
                new NocturnalAPI();

            _heartRate =
                new HeartRateAPI();

            // ===============================================
            // NEURAL
            // ===============================================

            _bpTrend =
                new BPTrendAPI();

            _cvdRisk =
                new CVDRiskAPI();

            _fhs =
                new FHSAPI();

            _hrTrend =
                new HRTrendAPI();

            _sistoTrend =
                new SistoTrendAPI();

            initTimer.Stop();
            InitializationMs = initTimer.Elapsed.TotalMilliseconds;
            InitializedAtUtc = DateTime.UtcNow;

            if (System.Threading.Interlocked.Increment(ref _instances) == 1)
            {
                FirstInitializationMs = InitializationMs;
            }

            Console.WriteLine(
                $"[MatlabService] instancia #{_instances} inicializada en {InitializationMs:F1} ms");
        }

        // ===================================================
        // BP LEVEL FUZZY
        // ===================================================

        public double BPLevel(
            double systolic,
            double diastolic)
        {
            try
            {
                MWNumericArray sys =
                    new MWNumericArray(
                        systolic);

                MWNumericArray dia =
                    new MWNumericArray(
                        diastolic);

                MWNumericArray result =
                    (MWNumericArray)
                    _bpLevel.RunBPLevel(
                        sys,
                        dia);

                return
                    result.ToScalarDouble();
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"BPLevel MATLAB error: {ex.Message}");
            }
        }

        // ===================================================
        // BP LOAD FUZZY
        // ===================================================

        public double BPLoad(
            double diurnal,
            double nocturnal)
        {
            try
            {
                MWNumericArray d =
                    new MWNumericArray(
                        diurnal);

                MWNumericArray n =
                    new MWNumericArray(
                        nocturnal);

                MWNumericArray result =
                    (MWNumericArray)
                    _bpLoad.RunBPLoad(
                        d,
                        n);

                return
                    result.ToScalarDouble();
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"BPLoad MATLAB error: {ex.Message}");
            }
        }

        //// ===================================================
        //// HEART RATE FUZZY
        //// ===================================================

        public double HeartRate(
            double age,
            double pulse)
        {
            try
            {
                MWNumericArray a =
                    new MWNumericArray(age);

                MWNumericArray p =
                    new MWNumericArray(pulse);

                MWNumericArray result =
                    (MWNumericArray)
                    _heartRate.RunHeartRate(
                        a,
                        p);

                return
                    result.ToScalarDouble();
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"HeartRate MATLAB error: {ex.Message}");
            }
        }

        // ===================================================
        // NOCTURNAL FUZZY
        // ===================================================

        public double NocturnalBP(
            double systolicRatio,
            double diastolicRatio)
        {
            try
            {
                MWNumericArray sys =
                    new MWNumericArray(
                        systolicRatio);

                MWNumericArray dia =
                    new MWNumericArray(
                        diastolicRatio);

                MWNumericArray result =
                    (MWNumericArray)
                    _nocturnal.RunNocturnalBP(
                        sys,
                        dia);

                return
                    result.ToScalarDouble();
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"NocturnalBP MATLAB error: {ex.Message}");
            }
        }

        // ===================================================
        // FHS
        // ===================================================

        public double FHS(
            double edad,
            double sexo,
            double bmi,
            double sys,
            double dia,
            double fuma,
            double padres)
        {
            try
            {
                MWNumericArray result =
                    (MWNumericArray)
                    _fhs.RunFHS(
                        new MWNumericArray(edad),
                        new MWNumericArray(sexo),
                        new MWNumericArray(bmi),
                        new MWNumericArray(sys),
                        new MWNumericArray(dia),
                        new MWNumericArray(fuma),
                        new MWNumericArray(padres)
                    );

                return
                    result.ToScalarDouble();
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"FHS MATLAB error: {ex.Message}");
            }
        }

        // ===================================================
        // CVD RISK
        // ===================================================

        public double[] CVDRisk(
            double sexo,
            double edad,
            double sys,
            double fuma,
            double diabetes,
            double bmi,
            double trata)
        {
            try
            {
                MWNumericArray result =
                    (MWNumericArray)
                    _cvdRisk.RunCVDRisk(
                        new MWNumericArray(sexo),
                        new MWNumericArray(edad),
                        new MWNumericArray(sys),
                        new MWNumericArray(fuma),
                        new MWNumericArray(diabetes),
                        new MWNumericArray(bmi),
                        new MWNumericArray(trata)
                    );

                return
                    result
                    .ToVector(
                        MWArrayComponent.Real)
                    .Cast<object>()
                    .Select(x =>
                        Convert.ToDouble(x))
                    .ToArray();
                                }
            catch (Exception ex)
            {
                throw new Exception(
                    $"CVDRisk MATLAB error: {ex.Message}");
            }
        }
        // ===================================================
        // HR TREND
        // ===================================================

        public double HRTrend(double[] pulse)
        {
            try
            {
                MWNumericArray result =
                    (MWNumericArray)
                    _hrTrend.RunHRTrend(
                        new MWNumericArray(
                            pulse));

                return
                    result.ToScalarDouble();
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"HRTrend MATLAB error: {ex.Message}");
            }
        }

        // ===================================================
        // SISTOLIC TREND
        // ===================================================

        public double SistoTrend(double[] systolic)
        {
            try
            {
                MWNumericArray result =
                    (MWNumericArray)
                    _sistoTrend.RunSistoTrend(
                        new MWNumericArray(
                            systolic));

                return
                    result.ToScalarDouble();
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"SistoTrend MATLAB error: {ex.Message}");
            }
        }

        // ===================================================
        // BP TREND
        // ===================================================

        public double[] BPTrend(
            int[] systolic,
            int[] diastolic,
            int[] pulse)
        {
            try
            {
                systolic =
                    NormalizeLength(
                        systolic,
                        47);

                diastolic =
                    NormalizeLength(
                        diastolic,
                        47);

                pulse =
                    NormalizeLength(
                        pulse,
                        47);
                double[] sysData =
                    systolic
                    .Select(x => (double)x)
                    .ToArray();

                double[] diaData =
                    diastolic
                    .Select(x => (double)x)
                    .ToArray();

                double[] pulData =
                    pulse
                    .Select(x => (double)x)
                    .ToArray();

                MWNumericArray sys =
                    new MWNumericArray(
                        sysData);

                MWNumericArray dia =
                    new MWNumericArray(
                        diaData);

                MWNumericArray pul =
                    new MWNumericArray(
                        pulData);

                MWArray output =
                    _bpTrend.RunBPTrend(
                        sys,
                        dia,
                        pul);

                double[,] values =
                    (double[,])
                    output.ToArray();

                return new double[]
                {
            values[0, 0],
            values[0, 1],
            values[0, 2]
                };
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"BPTrend MATLAB error: {ex.Message}");
            }
        }
        // ===================================================
        // NORMALIZE LENGTH
        // ===================================================

        private int[] NormalizeLength(
            int[] data,
            int targetLength)
        {
            // CORRECCION: antes, si no venian 47 valores, se repetia el ultimo
            // o se truncaba. MATLAB (NormalizeLength con interp1 lineal) hace
            // OTRA cosa, asi que en silencio se habria roto la equivalencia
            // con la referencia. Hoy el pipeline garantiza 47 lecturas; si
            // alguna vez no es asi, es preferible fallar a dar otro resultado.
            if (data.Length != targetLength)
            {
                throw new InvalidOperationException(
                    $"BPTrend espera {targetLength} lecturas y recibio {data.Length}.");
            }

            return data;
        }

        // ===================================================
        // DISPOSE
        // ===================================================

        public void Dispose()
        {
            _bpLevel?.Dispose();

            _bpLoad?.Dispose();

            _nocturnal?.Dispose();

            _heartRate?.Dispose();

            _bpTrend?.Dispose();

            _cvdRisk?.Dispose();

            _fhs?.Dispose();

            _hrTrend?.Dispose();

            _sistoTrend?.Dispose();
        }
    }
}
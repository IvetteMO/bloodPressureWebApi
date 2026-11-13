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
        // LIBERACION DE MEMORIA NATIVA
        //
        // Cada MWNumericArray reserva memoria FUERA del recolector de .NET
        // (en el MATLAB Runtime). Si no se hace Dispose, esa memoria solo se
        // libera cuando el GC decide finalizar el objeto, y en un servidor
        // que corre semanas el proceso crece sin control.
        //
        // Por eso cada entrada y cada salida se declara con "using var":
        // se liberan al terminar el metodo, despues de leer el resultado.
        // Los calculos y los mensajes de error son los mismos de antes.
        // ===================================================

        // ===================================================
        // BP LEVEL FUZZY
        // ===================================================

        public double BPLevel(
            double systolic,
            double diastolic)
        {
            try
            {
                using var sys = new MWNumericArray(systolic);
                using var dia = new MWNumericArray(diastolic);

                using var result = (MWNumericArray)_bpLevel.RunBPLevel(sys, dia);

                return result.ToScalarDouble();
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"BPLevel MATLAB error: {ex.Message}", ex);
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
                using var d = new MWNumericArray(diurnal);
                using var n = new MWNumericArray(nocturnal);

                using var result = (MWNumericArray)_bpLoad.RunBPLoad(d, n);

                return result.ToScalarDouble();
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"BPLoad MATLAB error: {ex.Message}", ex);
            }
        }

        // ===================================================
        // HEART RATE FUZZY
        // ===================================================

        public double HeartRate(
            double age,
            double pulse)
        {
            try
            {
                using var a = new MWNumericArray(age);
                using var p = new MWNumericArray(pulse);

                using var result = (MWNumericArray)_heartRate.RunHeartRate(a, p);

                return result.ToScalarDouble();
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"HeartRate MATLAB error: {ex.Message}", ex);
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
                using var sys = new MWNumericArray(systolicRatio);
                using var dia = new MWNumericArray(diastolicRatio);

                using var result = (MWNumericArray)_nocturnal.RunNocturnalBP(sys, dia);

                return result.ToScalarDouble();
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"NocturnalBP MATLAB error: {ex.Message}", ex);
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
                using var mEdad = new MWNumericArray(edad);
                using var mSexo = new MWNumericArray(sexo);
                using var mBmi = new MWNumericArray(bmi);
                using var mSys = new MWNumericArray(sys);
                using var mDia = new MWNumericArray(dia);
                using var mFuma = new MWNumericArray(fuma);
                using var mPadres = new MWNumericArray(padres);

                using var result = (MWNumericArray)_fhs.RunFHS(
                    mEdad, mSexo, mBmi, mSys, mDia, mFuma, mPadres);

                return result.ToScalarDouble();
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"FHS MATLAB error: {ex.Message}", ex);
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
                using var mSexo = new MWNumericArray(sexo);
                using var mEdad = new MWNumericArray(edad);
                using var mSys = new MWNumericArray(sys);
                using var mFuma = new MWNumericArray(fuma);
                using var mDiabetes = new MWNumericArray(diabetes);
                using var mBmi = new MWNumericArray(bmi);
                using var mTrata = new MWNumericArray(trata);

                using var result = (MWNumericArray)_cvdRisk.RunCVDRisk(
                    mSexo, mEdad, mSys, mFuma, mDiabetes, mBmi, mTrata);

                // ToArray() copia los valores a memoria administrada ANTES
                // de que "using" libere el arreglo de MATLAB.
                return result
                    .ToVector(MWArrayComponent.Real)
                    .Cast<object>()
                    .Select(x => Convert.ToDouble(x))
                    .ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"CVDRisk MATLAB error: {ex.Message}", ex);
            }
        }

        // ===================================================
        // HR TREND
        // ===================================================

        public double HRTrend(double[] pulse)
        {
            try
            {
                using var input = new MWNumericArray(pulse);

                using var result = (MWNumericArray)_hrTrend.RunHRTrend(input);

                return result.ToScalarDouble();
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"HRTrend MATLAB error: {ex.Message}", ex);
            }
        }

        // ===================================================
        // SISTOLIC TREND
        // ===================================================

        public double SistoTrend(double[] systolic)
        {
            try
            {
                using var input = new MWNumericArray(systolic);

                using var result = (MWNumericArray)_sistoTrend.RunSistoTrend(input);

                return result.ToScalarDouble();
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"SistoTrend MATLAB error: {ex.Message}", ex);
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
                systolic = NormalizeLength(systolic, 47);
                diastolic = NormalizeLength(diastolic, 47);
                pulse = NormalizeLength(pulse, 47);

                double[] sysData = systolic.Select(x => (double)x).ToArray();
                double[] diaData = diastolic.Select(x => (double)x).ToArray();
                double[] pulData = pulse.Select(x => (double)x).ToArray();

                using var sys = new MWNumericArray(sysData);
                using var dia = new MWNumericArray(diaData);
                using var pul = new MWNumericArray(pulData);

                using MWArray output = _bpTrend.RunBPTrend(sys, dia, pul);

                double[,] values = (double[,])output.ToArray();

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
                    $"BPTrend MATLAB error: {ex.Message}", ex);
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
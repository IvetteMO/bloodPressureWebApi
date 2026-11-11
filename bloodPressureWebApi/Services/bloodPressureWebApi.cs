using MathWorks.MATLAB.NET.Arrays;
using MySoftDiagnosisAssembly;

namespace bloodPressureWebApi.Services
{
    public class bloodPressureWebApi
    {
        private readonly BPLevelAPI _bpLevel;

        public bloodPressureWebApi()
        {
            _bpLevel = new BPLevelAPI();
        }

        public double BPLevel(double systolic,double diastolic)
        {
            MWNumericArray sys = new MWNumericArray(systolic);

            MWNumericArray dia =new MWNumericArray(diastolic);

            MWNumericArray result = (MWNumericArray)_bpLevel.RunBPLevel(sys, dia);

            return result.ToScalarDouble();
        }
    }
}

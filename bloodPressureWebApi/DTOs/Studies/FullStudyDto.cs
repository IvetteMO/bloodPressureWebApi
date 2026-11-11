using bloodPressureWebApi.DTOs.Readings;
using bloodPressureWebApi.DTOs.Patients;
using bloodPressureWebApi.DTOs.Diagnosis;

namespace bloodPressureWebApi.DTOs.Studies
{
    public class FullStudyDto
    {
        public PatientListDto? Patient { get; set; }

        public StudyDetailDto? Study { get; set; }

        public QuickDiagnosisResponseDto? Diagnosis { get; set; }

        public List<ChartReadingDto>Readings{ get; set; }= new();
    }
}
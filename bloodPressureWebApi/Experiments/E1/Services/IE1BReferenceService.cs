using bloodPressureWebApi.DTOs.E1;
using bloodPressureWebApi.DTOs.Experiments;

namespace bloodPressureWebApi.Services.Interfaces
{
    public interface IE1BReferenceService
    {
        Task<Dictionary<string, E1MatlabReferenceDto>>
            LoadReferenceAsync();

        bool Exists(string fileName);

        E1MatlabReferenceDto?
            GetReference(string fileName);

        IReadOnlyDictionary<string, E1MatlabReferenceDto>
            GetAll();
    }
}
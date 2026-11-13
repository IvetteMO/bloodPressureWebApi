using bloodPressureWebApi.Constants;
using bloodPressureWebApi.Infrastructure.Auth;

namespace bloodPressureWebApi.Tests.Infrastructure
{
    public class SmallRulesTests
    {
        [Theory]
        [InlineData("QUEUED", true)]
        [InlineData("processing", true)]
        [InlineData("AI_ANALYSIS", true)]
        [InlineData("COMPLETED", false)]
        [InlineData("FAILED", false)]
        // Los estudios viejos se quedaron en UPLOADED aunque tengan diagnóstico:
        // no deben mostrarse como "recalculando" ni re-encolarse al arrancar.
        [InlineData("UPLOADED", false)]
        [InlineData(null, false)]
        public void Only_real_in_progress_statuses_count_as_in_progress(string? status, bool expected)
        {
            Assert.Equal(expected, StudyProcessingStatus.IsInProgress(status));
        }

        [Fact]
        public void Temporary_passwords_are_long_unique_and_avoid_ambiguous_characters()
        {
            var passwords = Enumerable.Range(0, 200).Select(_ => PasswordGenerator.Generate()).ToList();

            Assert.All(passwords, p => Assert.Equal(14, p.Length));
            Assert.All(passwords, p => Assert.DoesNotContain(p, c => "0O1lI".Contains(c)));
            Assert.Equal(passwords.Count, passwords.Distinct().Count());
        }
    }
}

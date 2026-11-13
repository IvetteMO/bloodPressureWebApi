using bloodPressureWebApi.Constants;
using bloodPressureWebApi.Data;
using bloodPressureWebApi.Models;
using bloodPressureWebApi.Services;
using bloodPressureWebApi.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;

namespace bloodPressureWebApi.Tests.Auth
{
    public class AuthServiceTests
    {
        private const string Password = "Correcta-2026";

        private static (AuthService Service, AppUser User) Setup(
            ApplicationDbContext db,
            bool isActive = true)
        {
            var hasher = new PasswordHasher<AppUser>();

            var user = new AppUser
            {
                Email = "medico@hospital.mx",
                DisplayName = "Dra. Prueba",
                Role = AppRoles.Medico,
                IsActive = isActive
            };

            user.PasswordHash = hasher.HashPassword(user, Password);

            db.AppUsers.Add(user);
            db.SaveChanges();

            return (new AuthService(db, hasher, NullLogger<AuthService>.Instance), user);
        }

        [Fact]
        public async Task Valid_credentials_sign_in_and_email_is_case_insensitive()
        {
            using var db = TestDb.Create();
            var (service, _) = Setup(db);

            var result = await service.ValidateCredentialsAsync("  MEDICO@Hospital.mx ", Password, CancellationToken.None);

            Assert.Equal(LoginOutcome.Success, result.Outcome);
            Assert.NotNull(result.User!.LastLoginAtUtc);
        }

        [Fact]
        public async Task Wrong_password_and_unknown_email_give_the_same_answer()
        {
            using var db = TestDb.Create();
            var (service, _) = Setup(db);

            var wrongPassword = await service.ValidateCredentialsAsync("medico@hospital.mx", "otra", CancellationToken.None);
            var unknownEmail = await service.ValidateCredentialsAsync("nadie@hospital.mx", Password, CancellationToken.None);

            Assert.Equal(LoginOutcome.InvalidCredentials, wrongPassword.Outcome);
            Assert.Equal(LoginOutcome.InvalidCredentials, unknownEmail.Outcome);
        }

        [Fact]
        public async Task Five_failed_attempts_lock_the_account_even_with_the_right_password()
        {
            using var db = TestDb.Create();
            var (service, _) = Setup(db);

            for (var attempt = 1; attempt < AuthService.MaxFailedAttempts; attempt++)
            {
                await service.ValidateCredentialsAsync("medico@hospital.mx", "mal", CancellationToken.None);
            }

            var fifth = await service.ValidateCredentialsAsync("medico@hospital.mx", "mal", CancellationToken.None);
            var thenCorrect = await service.ValidateCredentialsAsync("medico@hospital.mx", Password, CancellationToken.None);

            Assert.Equal(LoginOutcome.LockedOut, fifth.Outcome);
            Assert.Equal(LoginOutcome.LockedOut, thenCorrect.Outcome);
        }

        [Fact]
        public async Task Inactive_users_cannot_sign_in()
        {
            using var db = TestDb.Create();
            var (service, _) = Setup(db, isActive: false);

            var result = await service.ValidateCredentialsAsync("medico@hospital.mx", Password, CancellationToken.None);

            Assert.Equal(LoginOutcome.InvalidCredentials, result.Outcome);
        }

        [Fact]
        public async Task Changing_the_password_rotates_the_security_stamp_and_clears_the_temporary_flag()
        {
            using var db = TestDb.Create();
            var (service, user) = Setup(db);
            user.MustChangePassword = true;
            db.SaveChanges();
            var oldStamp = user.SecurityStamp;

            var changed = await service.ChangePasswordAsync(user.AppUserId, Password, "Nueva-Segura-2026", CancellationToken.None);

            Assert.NotNull(changed);
            Assert.False(changed!.MustChangePassword);
            Assert.NotEqual(oldStamp, changed.SecurityStamp);

            var withNew = await service.ValidateCredentialsAsync("medico@hospital.mx", "Nueva-Segura-2026", CancellationToken.None);
            Assert.Equal(LoginOutcome.Success, withNew.Outcome);
        }

        [Fact]
        public async Task Changing_the_password_requires_the_current_one()
        {
            using var db = TestDb.Create();
            var (service, user) = Setup(db);

            var changed = await service.ChangePasswordAsync(user.AppUserId, "no-es-la-actual", "Nueva-Segura-2026", CancellationToken.None);

            Assert.Null(changed);
        }
    }
}

using AtlasRFID.Manager.Api.Dtos;
using AtlasRFID.Manager.Api.Repositories;
using AtlasRFID.Manager.Api.Security;
using Microsoft.AspNetCore.Mvc;

namespace AtlasRFID.Manager.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserRepository _users;
        private readonly JwtTokenService _tokens;

        public AuthController(UserRepository users, JwtTokenService tokens)
        {
            _users = users;
            _tokens = tokens;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var user = await _users.FindForLoginAsync(request.UserNameOrEmail);
            if (user == null)
                return Unauthorized(new { error = "invalid_credentials" });

            if (!user.IsActive || user.IsDeleted)
                return Unauthorized(new { error = "user_disabled" });

            if (user.LockoutUntil.HasValue && user.LockoutUntil.Value > DateTime.UtcNow)
                return Unauthorized(new { error = "locked_out", until = user.LockoutUntil });

            // Verify password (your stored hash is bcrypt, so this matches your DB)
            var ok = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!ok)
            {
                var failed = user.FailedLoginCount + 1;

                // Simple lockout policy: 5 failed -> 10 minutes
                DateTime? lockoutUntil = null;
                if (failed >= 5)
                    lockoutUntil = DateTime.UtcNow.AddMinutes(10);

                await _users.RegisterFailedLoginAsync(user.Id, failed, lockoutUntil);
                return Unauthorized(new { error = "invalid_credentials" });
            }

            await _users.RegisterSuccessfulLoginAsync(user.Id);

            var (token, expiresIn) = _tokens.CreateAccessToken(user);
            return Ok(new LoginResponse
            {
                AccessToken = token,
                ExpiresInSeconds = expiresIn
            });
        }
    }
}

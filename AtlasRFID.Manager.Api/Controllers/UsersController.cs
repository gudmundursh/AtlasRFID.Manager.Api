using AtlasRFID.Manager.Api.Dtos;
using AtlasRFID.Manager.Api.Repositories;
using AtlasRFID.Manager.Api.Security;
using AtlasRFID.Manager.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AtlasRFID.Manager.Api.Controllers
{
    [Authorize(Policy = "CompanyAdminOnly")]
    [ApiController]
    [Route("api/users")]
    public class UsersController : AppControllerBase
    {
        private readonly UserRepository _users;
        private readonly ITenantProvider _tenant;
        private readonly IAuditLogger _audit;
        private readonly ICorrelationIdProvider _corr;

        public UsersController(
            UserRepository users,
            ITenantProvider tenant,
            IAuditLogger audit,
            ICorrelationIdProvider corr)
        {
            _users = users;
            _tenant = tenant;
            _audit = audit;
            _corr = corr;
        }

        [HttpPost("{id:guid}/reset-password")]
        public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
                return BadRequest(new { error = "password_too_short_min_8" });

            var updatedByUserId = GetUserIdOrNull();
            if (updatedByUserId == null)
                return Unauthorized(new { error = "missing_user_id_claim" });

            var target = await _users.GetSecurityInfoAsync(id);
            if (target == null) return NotFound();

            // Company admins can never reset a SuperAdmin
            if (target.Value.IsSuperAdmin)
                return Forbid();

            var myCompanyId = _tenant.GetCompanyId();

            // Must be in same company
            if (target.Value.CompanyId == null || target.Value.CompanyId.Value != myCompanyId)
                return Forbid();

            var hash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _users.ResetPasswordAsync(id, hash, updatedByUserId.Value);

            // audit (safe)
            await AuditAsync(
                _audit, _corr,
                companyId: myCompanyId,
                action: "ResetPassword",
                entityType: "User",
                entityId: id,
                before: new { Id = id, UserName = target.Value.UserName },
                after: new { Id = id, UserName = target.Value.UserName, PasswordUpdatedAt = System.DateTime.UtcNow },
                message: $"Password reset for user '{target.Value.UserName}'"
            );

            return Ok(new { ok = true });
        }
    }
}

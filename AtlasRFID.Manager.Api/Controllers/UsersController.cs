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
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var myCompanyId = _tenant.GetCompanyId();
            var rows = await _users.GetByCompanyAsync(myCompanyId);
            return Ok(rows);
        }
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTenantUserRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var updatedByUserId = GetUserIdOrNull();
            if (updatedByUserId == null)
                return Unauthorized(new { error = "missing_user_id_claim" });

            var myCompanyId = _tenant.GetCompanyId();

            // "before" snapshot (security info is enough + admin view if you want)
            var before = await _users.GetByIdForAdminAsync(id);
            if (before == null) return NotFound();

            // Must be in same company
            if ((Guid?)before.CompanyId != myCompanyId) return Forbid();
            if ((bool)before.IsSuperAdmin) return Forbid();

            await _users.UpdateTenantAsync(
                id,
                myCompanyId,
                request.Email,
                request.DisplayName,
                request.IsCompanyAdmin,
                request.IsActive,
                updatedByUserId.Value);

            var after = await _users.GetByIdForAdminAsync(id);
            if (after == null) return StatusCode(500, new { error = "update_user_failed" });

            await AuditAsync(
                _audit, _corr,
                companyId: myCompanyId,
                action: "Update",
                entityType: "User",
                entityId: id,
                before: before,
                after: after,
                message: $"Updated user '{after.UserName}'"
            );

            return Ok(after);
        }
        [HttpPost("{id:guid}/deactivate")]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            var updatedByUserId = GetUserIdOrNull();
            if (updatedByUserId == null)
                return Unauthorized(new { error = "missing_user_id_claim" });

            var myCompanyId = _tenant.GetCompanyId();

            var before = await _users.GetByIdForAdminAsync(id);
            if (before == null) return NotFound();

            if ((Guid?)before.CompanyId != myCompanyId) return Forbid();
            if ((bool)before.IsSuperAdmin) return Forbid();

            await _users.SetActiveTenantAsync(id, myCompanyId, false, updatedByUserId.Value);

            var after = await _users.GetByIdForAdminAsync(id);
            if (after == null) return StatusCode(500, new { error = "deactivate_user_failed" });

            await AuditAsync(
                _audit, _corr,
                companyId: myCompanyId,
                action: "Deactivate",
                entityType: "User",
                entityId: id,
                before: before,
                after: after,
                message: $"Deactivated user '{after.UserName}'"
            );

            return Ok(after);
        }

        [HttpPost("{id:guid}/activate")]
        public async Task<IActionResult> Activate(Guid id)
        {
            var updatedByUserId = GetUserIdOrNull();
            if (updatedByUserId == null)
                return Unauthorized(new { error = "missing_user_id_claim" });

            var myCompanyId = _tenant.GetCompanyId();

            var before = await _users.GetByIdForAdminAsync(id);
            if (before == null) return NotFound();

            if ((Guid?)before.CompanyId != myCompanyId) return Forbid();
            if ((bool)before.IsSuperAdmin) return Forbid();

            await _users.SetActiveTenantAsync(id, myCompanyId, true, updatedByUserId.Value);

            var after = await _users.GetByIdForAdminAsync(id);
            if (after == null) return StatusCode(500, new { error = "activate_user_failed" });

            await AuditAsync(
                _audit, _corr,
                companyId: myCompanyId,
                action: "Activate",
                entityType: "User",
                entityId: id,
                before: before,
                after: after,
                message: $"Activated user '{after.UserName}'"
            );

            return Ok(after);
        }

    }
}

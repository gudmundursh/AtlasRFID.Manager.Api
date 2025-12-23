using AtlasRFID.Manager.Api.Dtos;
using AtlasRFID.Manager.Api.Repositories;
using AtlasRFID.Manager.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AtlasRFID.Manager.Api.Controllers
{
    [Authorize(Policy = "SuperAdminOnly")]
    [ApiController]
    [Route("api/system/users")]
    public class SystemUsersController : AppControllerBase
    {
        private readonly UserRepository _users;
        private readonly IAuditLogger _audit;
        private readonly ICorrelationIdProvider _corr;

        public SystemUsersController(
            UserRepository users,
            IAuditLogger audit,
            ICorrelationIdProvider corr)
        {
            _users = users;
            _audit = audit;
            _corr = corr;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            if (!request.IsSuperAdmin && string.IsNullOrWhiteSpace(request.CompanyId))
                return BadRequest(new { error = "company_id_required_for_non_superadmin" });

            if (!string.IsNullOrWhiteSpace(request.CompanyId) && !Guid.TryParse(request.CompanyId, out _))
                return BadRequest(new { error = "invalid_company_id" });

            if (await _users.UserNameOrEmailExistsAsync(request.UserName, request.Email))
                return Conflict(new { error = "username_or_email_exists" });

            var createdByUserId = GetUserIdOrNull();
            if (createdByUserId == null)
                return Unauthorized(new { error = "missing_user_id_claim" });

            Guid? companyId = null;
            if (!string.IsNullOrWhiteSpace(request.CompanyId))
                companyId = Guid.Parse(request.CompanyId);

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var newId = await _users.CreateAsync(
                companyId,
                request.UserName,
                request.Email,
                request.DisplayName,
                passwordHash,
                request.IsSuperAdmin,
                request.IsCompanyAdmin,
                request.IsActive,
                createdByUserId.Value);

            // ---- audit (SAFE) ----
            var safeAfter = new
            {
                Id = newId,
                CompanyId = companyId,
                request.UserName,
                request.Email,
                request.DisplayName,
                request.IsSuperAdmin,
                request.IsCompanyAdmin,
                request.IsActive
            };

            await AuditAsync(
                _audit, _corr,
                companyId: companyId,
                action: "Create",
                entityType: "User",
                entityId: newId,
                before: null,
                after: safeAfter,
                message: $"Created user '{request.UserName}'"
            );

            return Ok(new { id = newId });
        }
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var updatedByUserId = GetUserIdOrNull();
            if (updatedByUserId == null)
                return Unauthorized(new { error = "missing_user_id_claim" });

            var before = await _users.GetByIdForAdminAsync(id);
            if (before == null) return NotFound();

            await _users.UpdateAsync(
                id,
                request.Email,
                request.DisplayName,
                request.IsSuperAdmin,
                request.IsCompanyAdmin,
                request.IsActive,
                updatedByUserId.Value);

            var after = await _users.GetByIdForAdminAsync(id);
            if (after == null) return StatusCode(500, new { error = "update_user_failed" });

            // companyId for audit = the target user’s company
            Guid? companyId = (Guid?)after.CompanyId;

            await AuditAsync(
                _audit, _corr,
                companyId: companyId,
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

            var before = await _users.GetByIdForAdminAsync(id);
            if (before == null) return NotFound();

            // already inactive
            if ((bool)before.IsActive == false) return Ok(before);

            await _users.SetActiveAsync(id, false, updatedByUserId.Value);

            var after = await _users.GetByIdForAdminAsync(id);
            if (after == null) return StatusCode(500, new { error = "deactivate_user_failed" });

            Guid? companyId = (Guid?)after.CompanyId;

            await AuditAsync(
                _audit, _corr,
                companyId: companyId,
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

            var before = await _users.GetByIdForAdminAsync(id);
            if (before == null) return NotFound();

            // already active
            if ((bool)before.IsActive == true) return Ok(before);

            await _users.SetActiveAsync(id, true, updatedByUserId.Value);

            var after = await _users.GetByIdForAdminAsync(id);
            if (after == null) return StatusCode(500, new { error = "activate_user_failed" });

            Guid? companyId = (Guid?)after.CompanyId;

            await AuditAsync(
                _audit, _corr,
                companyId: companyId,
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

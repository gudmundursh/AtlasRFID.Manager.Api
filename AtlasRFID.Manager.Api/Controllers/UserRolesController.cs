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
    [Route("api/users/{userId:guid}/roles")]
    public class UserRolesController : AppControllerBase
    {
        private readonly RbacRepository _rbac;
        private readonly UserRepository _users;
        private readonly ITenantProvider _tenant;
        private readonly IAuditLogger _audit;
        private readonly ICorrelationIdProvider _corr;

        public UserRolesController(
            RbacRepository rbac,
            UserRepository users,
            ITenantProvider tenant,
            IAuditLogger audit,
            ICorrelationIdProvider corr)
        {
            _rbac = rbac;
            _users = users;
            _tenant = tenant;
            _audit = audit;
            _corr = corr;
        }

        [HttpGet]
        public async Task<IActionResult> GetAssignments(Guid userId)
        {
            // Ensure user is in my company
            var info = await _users.GetSecurityInfoAsync(userId);
            if (info == null) return NotFound();

            var myCompanyId = _tenant.GetCompanyId();
            if (info.Value.CompanyId != myCompanyId) return Forbid();
            if (info.Value.IsSuperAdmin) return Forbid();

            var rows = await _rbac.GetAssignmentsForUserAsync(userId);
            return Ok(rows);
        }

        [HttpPost]
        public async Task<IActionResult> Assign(Guid userId, [FromBody] AssignRoleRequest request)
        {
            var actorId = GetUserIdOrNull();
            if (actorId == null) return Unauthorized(new { error = "missing_user_id_claim" });

            // Ensure user is in my company
            var userInfo = await _users.GetSecurityInfoAsync(userId);
            if (userInfo == null) return NotFound();

            var myCompanyId = _tenant.GetCompanyId();
            if (userInfo.Value.CompanyId != myCompanyId) return Forbid();
            if (userInfo.Value.IsSuperAdmin) return Forbid();

            // Ensure role is in my company
            var role = await _rbac.GetRoleByIdAsync(request.RoleId);
            if (role == null) return NotFound(new { error = "role_not_found" });
            if (role.CompanyId != myCompanyId) return Forbid();

            var assignmentId = await _rbac.AssignRoleAsync(userId, request.RoleId, request.ScopeType, request.ScopeId, actorId.Value);

            await AuditAsync(_audit, _corr,
                companyId: myCompanyId,
                action: "AssignRole",
                entityType: "UserRoleAssignment",
                entityId: assignmentId,
                before: null,
                after: new
                {
                    AssignmentId = assignmentId,
                    UserId = userId,
                    UserName = userInfo.Value.UserName,
                    RoleId = role.Id,
                    RoleCode = role.Code,
                    request.ScopeType,
                    request.ScopeId
                },
                message: $"Assigned role '{role.Code}' to '{userInfo.Value.UserName}'"
            );

            return Ok(new { id = assignmentId });
        }

        [HttpDelete("{assignmentId:guid}")]
        public async Task<IActionResult> Remove(Guid userId, Guid assignmentId)
        {
            var actorId = GetUserIdOrNull();
            if (actorId == null) return Unauthorized(new { error = "missing_user_id_claim" });

            // Ensure user is in my company
            var userInfo = await _users.GetSecurityInfoAsync(userId);
            if (userInfo == null) return NotFound();

            var myCompanyId = _tenant.GetCompanyId();
            if (userInfo.Value.CompanyId != myCompanyId) return Forbid();
            if (userInfo.Value.IsSuperAdmin) return Forbid();

            // fetch before snapshot (optional but nice)
            var before = (await _rbac.GetAssignmentsForUserAsync(userId)).FirstOrDefault(a => a.Id == assignmentId);

            await _rbac.RemoveAssignmentAsync(assignmentId);

            await AuditAsync(_audit, _corr,
                companyId: myCompanyId,
                action: "RemoveRole",
                entityType: "UserRoleAssignment",
                entityId: assignmentId,
                before: before,
                after: null,
                message: $"Removed role assignment from '{userInfo.Value.UserName}'"
            );

            return Ok(new { ok = true });
        }

    }
}

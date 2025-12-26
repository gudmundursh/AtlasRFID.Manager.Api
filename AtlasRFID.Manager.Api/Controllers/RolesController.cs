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
    [Route("api/roles")]
    public class RolesController : AppControllerBase
    {
        private readonly RbacRepository _rbac;
        private readonly ITenantProvider _tenant;
        private readonly IAuditLogger _audit;
        private readonly ICorrelationIdProvider _corr;

        public RolesController(RbacRepository rbac, ITenantProvider tenant, IAuditLogger audit, ICorrelationIdProvider corr)
        {
            _rbac = rbac;
            _tenant = tenant;
            _audit = audit;
            _corr = corr;
        }

        [HttpGet("permissions")]
        public async Task<IActionResult> GetPermissions()
        {
            var perms = await _rbac.GetPermissionsAsync();
            return Ok(perms);
        }

        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            var companyId = _tenant.GetCompanyId();
            var roles = await _rbac.GetCompanyRolesAsync(companyId);
            return Ok(roles);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRoleRequest request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = GetUserIdOrNull();
            if (userId == null) return Unauthorized(new { error = "missing_user_id_claim" });

            var companyId = _tenant.GetCompanyId();

            var id = await _rbac.CreateRoleAsync(companyId, request.Code.Trim(), request.Name.Trim(), request.Description, userId.Value);

            await AuditAsync(_audit, _corr,
                companyId: companyId,
                action: "Create",
                entityType: "Role",
                entityId: id,
                before: null,
                after: new { Id = id, request.Code, request.Name, request.Description },
                message: $"Created role '{request.Code}'"
            );

            return Ok(new { id });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = GetUserIdOrNull();
            if (userId == null) return Unauthorized(new { error = "missing_user_id_claim" });

            var companyId = _tenant.GetCompanyId();

            var beforeRole = await _rbac.GetRoleByIdAsync(id);
            if (beforeRole == null) return NotFound();
            if (beforeRole.CompanyId != companyId) return Forbid();

            var beforePerms = await _rbac.GetRolePermissionCodesAsync(id);

            await _rbac.UpdateRoleAsync(id, companyId, request.Name.Trim(), request.Description, request.IsActive, userId.Value);

            // permissions update optional here; we’ll do it properly in next step after we add resolver by code -> id
            // (for now, skip PermissionCodes or handle IDs later)
            var afterRole = await _rbac.GetRoleByIdAsync(id);
            var afterPerms = await _rbac.GetRolePermissionCodesAsync(id);

            await AuditAsync(_audit, _corr,
                companyId: companyId,
                action: "Update",
                entityType: "Role",
                entityId: id,
                before: new { role = beforeRole, permissions = beforePerms },
                after: new { role = afterRole, permissions = afterPerms },
                message: $"Updated role '{beforeRole.Code}'"
            );

            return Ok(afterRole);
        }
    }
}

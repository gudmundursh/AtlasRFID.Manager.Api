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
        private readonly IPermissionService _perm;


        public RolesController(RbacRepository rbac, ITenantProvider tenant, IAuditLogger audit, ICorrelationIdProvider corr, IPermissionService perm)
        {
            _rbac = rbac;
            _tenant = tenant;
            _audit = audit;
            _corr = corr;
            _perm = perm;
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
        [HttpPut("{id:guid}/permissions")]
        public async Task<IActionResult> SetPermissions(Guid id, [FromBody] SetRolePermissionsRequest request)
        {
            var userId = GetUserIdOrNull();
            if (userId == null) return Unauthorized(new { error = "missing_user_id_claim" });

            var companyId = _tenant.GetCompanyId();

            var role = await _rbac.GetRoleByIdAsync(id);
            if (role == null) return NotFound();
            if (role.CompanyId != companyId) return Forbid();

            var beforePerms = (await _rbac.GetRolePermissionCodesAsync(id)).ToList();

            var requestedCodes = request.PermissionCodes ?? new List<string>();
            var map = await _rbac.GetPermissionIdsByCodeAsync(requestedCodes);

            // validate: if user asks for unknown permission codes, reject
            var normalizedRequested = requestedCodes
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var missing = normalizedRequested
                .Where(c => !map.ContainsKey(c))
                .ToList();

            if (missing.Count > 0)
                return BadRequest(new { error = "unknown_permission_codes", codes = missing });

            await _rbac.SetRolePermissionsAsync(id, map.Values);

            var afterPerms = (await _rbac.GetRolePermissionCodesAsync(id)).ToList();

            await AuditAsync(_audit, _corr,
                companyId: companyId,
                action: "SetPermissions",
                entityType: "Role",
                entityId: id,
                before: new { role = new { role.Id, role.Code, role.Name }, permissions = beforePerms },
                after: new { role = new { role.Id, role.Code, role.Name }, permissions = afterPerms },
                message: $"Updated permissions for role '{role.Code}'"
            );

            return Ok(new { roleId = id, permissions = afterPerms });
        }
        [HttpGet("{id:guid}/permissions")]
        public async Task<IActionResult> GetRolePermissions(Guid id)
        {
            var companyId = _tenant.GetCompanyId();

            var role = await _rbac.GetRoleByIdAsync(id);
            if (role == null) return NotFound();
            if (role.CompanyId != companyId) return Forbid();

            var codes = await _rbac.GetRolePermissionCodesAsync(id);
            return Ok(codes);
        }
        [HttpGet("{roleId:guid}/scopes/{scopeType}/{scopeId:guid}/permissions")]
        public async Task<IActionResult> GetScopedPermissions(Guid roleId, string scopeType, Guid scopeId)
        {
            var userId = GetUserIdOrNull();
            if (userId == null) return Unauthorized(new { error = "missing_user_id_claim" });

            // permission behind it:
            var canManage = await _perm.HasAsync(userId.Value, "rbac.scopedPermissions.manage");
            if (!canManage) return Forbid();

            var companyId = _tenant.GetCompanyId();

            var role = await _rbac.GetRoleByIdAsync(roleId);
            if (role == null) return NotFound();
            if (role.CompanyId != companyId) return Forbid();

            var effects = (await _rbac.GetScopedRolePermissionEffectsAsync(roleId, scopeType, scopeId)).ToList();

            var allow = effects.Where(x => string.Equals(x.Effect, "Allow", StringComparison.OrdinalIgnoreCase)).Select(x => x.Code).ToList();
            var deny = effects.Where(x => string.Equals(x.Effect, "Deny", StringComparison.OrdinalIgnoreCase)).Select(x => x.Code).ToList();

            return Ok(new { roleId, scopeType, scopeId, allow, deny });
        }
        [HttpPut("{roleId:guid}/scopes/{scopeType}/{scopeId:guid}/permissions")]
        public async Task<IActionResult> SetScopedPermissions(Guid roleId, string scopeType, Guid scopeId, [FromBody] SetScopedRolePermissionsRequest request)
        {
            var userId = GetUserIdOrNull();
            if (userId == null) return Unauthorized(new { error = "missing_user_id_claim" });

            var canManage = await _perm.HasAsync(userId.Value, "rbac.scopedPermissions.manage");
            if (!canManage) return Forbid();

            var companyId = _tenant.GetCompanyId();

            var role = await _rbac.GetRoleByIdAsync(roleId);
            if (role == null) return NotFound();
            if (role.CompanyId != companyId) return Forbid();

            // Validate permission codes exist
            var allCodes = request.Allow.Concat(request.Deny).ToList();
            var map = await _rbac.GetPermissionIdsByCodeAsync(allCodes);

            var normalized = allCodes.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var missing = normalized.Where(c => !map.ContainsKey(c)).ToList();
            if (missing.Count > 0)
                return BadRequest(new { error = "unknown_permission_codes", codes = missing });

            var allowIds = request.Allow.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => map[c.Trim()]).ToList();
            var denyIds = request.Deny.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => map[c.Trim()]).ToList();

            // BEFORE snapshot
            var before = (await _rbac.GetScopedRolePermissionEffectsAsync(roleId, scopeType, scopeId)).ToList();

            await _rbac.SetScopedRolePermissionsAsync(roleId, scopeType, scopeId, allowIds, denyIds, userId.Value);

            // AFTER snapshot
            var after = (await _rbac.GetScopedRolePermissionEffectsAsync(roleId, scopeType, scopeId)).ToList();

            await AuditAsync(_audit, _corr,
                companyId: companyId,
                action: "SetScopedPermissions",
                entityType: "Role",
                entityId: roleId,
                before: new { scopeType, scopeId, permissions = before },
                after: new { scopeType, scopeId, permissions = after },
                message: $"Updated scoped permissions for role '{role.Code}' ({scopeType}:{scopeId})"
            );

            return Ok(new { ok = true });
        }

    }
}

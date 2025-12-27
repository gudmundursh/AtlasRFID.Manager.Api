using AtlasRFID.Manager.Api.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AtlasRFID.Manager.Api.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly RbacRepository _rbac;

        public PermissionService(RbacRepository rbac)
        {
            _rbac = rbac;
        }

        public async Task<bool> HasAsync(Guid userId, string permissionCode, string? scopeType = null, Guid? scopeId = null)
        {
            // 1) Get assignments for user
            var assignments = (await _rbac.GetAssignmentsForUserAsync(userId)).ToList();
            if (assignments.Count == 0) return false;

            // 2) choose matching assignments:
            // - exact scope match OR (if scope requested) allow global assignments too
            var matching = new List<dynamic>();

            foreach (var a in assignments)
            {
                string aScopeType = a.ScopeType as string;
                Guid? aScopeId = a.ScopeId as Guid?;

                if (scopeType == null && scopeId == null)
                {
                    // global check: include only global assignments
                    if (aScopeType == null && aScopeId == null) matching.Add(a);
                }
                else
                {
                    // scoped check: include exact scoped + global
                    if (aScopeType == null && aScopeId == null) matching.Add(a);
                    else if (string.Equals(aScopeType, scopeType, StringComparison.OrdinalIgnoreCase) && aScopeId == scopeId)
                        matching.Add(a);
                }
            }

            if (matching.Count == 0) return false;

            // 3) Deny wins, then Allow
            bool anyAllow = false;

            foreach (var a in matching)
            {
                Guid roleId = a.RoleId;

                // If a scope is requested, check scoped overrides first
                if (scopeType != null && scopeId != null)
                {
                    var scoped = (await _rbac.GetScopedRolePermissionEffectsAsync(roleId, scopeType, scopeId.Value)).ToList();
                    if (scoped.Count > 0)
                    {
                        // scoped truth exists for this role+scope: use it (deny beats allow)
                        foreach (var sp in scoped)
                        {
                            if (!string.Equals(sp.Code, permissionCode, StringComparison.OrdinalIgnoreCase))
                                continue;

                            if (string.Equals(sp.Effect, "Deny", StringComparison.OrdinalIgnoreCase))
                                return false;

                            if (string.Equals(sp.Effect, "Allow", StringComparison.OrdinalIgnoreCase))
                                anyAllow = true;
                        }

                        // NOTE: if scoped exists but doesn’t mention this permission -> treat as not allowed for this role+scope
                        continue;
                    }
                }

                // fallback to global role permissions
                var globalCodes = await _rbac.GetRolePermissionCodesAsync(roleId);
                if (globalCodes.Any(c => string.Equals(c, permissionCode, StringComparison.OrdinalIgnoreCase)))
                    anyAllow = true;
            }

            return anyAllow;
        }
    }
}

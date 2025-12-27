using Microsoft.AspNetCore.Mvc;

namespace AtlasRFID.Manager.Api.Services
{
    public interface IPermissionService
    {
        Task<bool> HasAsync(Guid userId, string permissionCode, string? scopeType = null, Guid? scopeId = null);
    }
}

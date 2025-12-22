using AtlasRFID.Manager.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AtlasRFID.Manager.Api.Controllers
{
    public abstract class AppControllerBase : ControllerBase
    {
        protected Guid? GetUserIdOrNull()
        {
            var userIdStr = User.FindFirst("user_id")?.Value;
            return Guid.TryParse(userIdStr, out var id) ? id : (Guid?)null;
        }

        protected async Task AuditAsync(
            IAuditLogger audit,
            ICorrelationIdProvider corr,
            Guid? companyId,
            string action,
            string entityType,
            Guid? entityId,
            object before,
            object after,
            string message)
        {
            await audit.WriteAsync(
                companyId: companyId,
                userId: GetUserIdOrNull(),
                action: action,
                entityType: entityType,
                entityId: entityId,
                before: before,
                after: after,
                message: message,
                correlationId: corr.Get()
            );
        }
    }
}

using System.Security.Claims;

namespace AtlasRFID.Manager.Api.Security
{
    public class JwtOrSingleTenantProvider : ITenantProvider
    {
        private readonly IHttpContextAccessor _http;
        private readonly Guid? _singleTenantCompanyId;

        public JwtOrSingleTenantProvider(IHttpContextAccessor http, IConfiguration configuration)
        {
            _http = http;

            var value = configuration["SingleTenant:CompanyId"];
            if (Guid.TryParse(value, out var parsed))
                _singleTenantCompanyId = parsed;
        }

        public Guid GetCompanyId()
        {
            // 1) Prefer JWT claim (cloud mode)
            var ctx = _http.HttpContext;
            var claim = ctx?.User?.FindFirstValue("company_id");
            if (Guid.TryParse(claim, out var fromJwt))
                return fromJwt;

            // 2) Fallback to single-tenant config (on-prem mode)
            if (_singleTenantCompanyId.HasValue)
                return _singleTenantCompanyId.Value;

            // 3) If neither exists, the request has no tenant context
            throw new InvalidOperationException("No CompanyId available (missing JWT company_id and SingleTenant:CompanyId).");
        }

        public bool IsSystemContext()
        {
            var ctx = _http.HttpContext;
            var isSuper = string.Equals(ctx?.User?.FindFirstValue("is_super_admin"), "true", StringComparison.OrdinalIgnoreCase);

            // System context = super admin AND no tenant claim AND no single-tenant fallback
            var hasCompanyClaim = Guid.TryParse(ctx?.User?.FindFirstValue("company_id"), out _);
            return isSuper && !hasCompanyClaim && !_singleTenantCompanyId.HasValue;
        }
    }
}

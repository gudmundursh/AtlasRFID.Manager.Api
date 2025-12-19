using Microsoft.Extensions.Configuration;

namespace AtlasRFID.Manager.Api.Security
{
    public class SingleTenantProvider : ITenantProvider
    {
        private readonly Guid _companyId;

        public SingleTenantProvider(IConfiguration configuration)
        {
            var value = configuration["SingleTenant:CompanyId"];
            if (!Guid.TryParse(value, out _companyId))
                throw new InvalidOperationException("SingleTenant:CompanyId is not configured");
        }

        public Guid GetCompanyId() => _companyId;

        public bool IsSystemContext() => false;
    }
}

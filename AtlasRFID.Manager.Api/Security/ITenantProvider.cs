namespace AtlasRFID.Manager.Api.Security
{
    public interface ITenantProvider
    {
        Guid GetCompanyId();    //“Which company is this request for?”
        bool IsSystemContext(); //“Is this request being made in a system-wide context (not tied to any specific company)?”
    }
}

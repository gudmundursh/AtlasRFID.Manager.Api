namespace AtlasRFID.Manager.Api.Dtos
{
    public class MeResponse
    {
        public string UserId { get; set; }
        public string CompanyId { get; set; } // null for super-user
        public bool IsSuperAdmin { get; set; }
        public bool IsCompanyAdmin { get; set; }
    }
}

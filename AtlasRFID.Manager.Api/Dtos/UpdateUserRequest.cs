namespace AtlasRFID.Manager.Api.Dtos
{
    public class UpdateUserRequest
    {
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public bool IsSuperAdmin { get; set; }
        public bool IsCompanyAdmin { get; set; }
        public bool IsActive { get; set; }
    }
}

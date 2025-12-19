namespace AtlasRFID.Manager.Api.Models
{
    public class UserAuthRow
    {
        public Guid Id { get; set; }
        public Guid? CompanyId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public bool IsSuperAdmin { get; set; }
        public bool IsCompanyAdmin { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public int FailedLoginCount { get; set; }
        public DateTime? LockoutUntil { get; set; }
    }
}

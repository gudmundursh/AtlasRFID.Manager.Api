using System.ComponentModel.DataAnnotations;

namespace AtlasRFID.Manager.Api.Dtos
{
    public class CreateUserRequest
    {
        [Required] public string UserName { get; set; }
        [Required, EmailAddress] public string Email { get; set; }
        [Required] public string DisplayName { get; set; }
        [Required, MinLength(8)] public string Password { get; set; }

        // For normal users this MUST be set
        public string CompanyId { get; set; }

        public bool IsCompanyAdmin { get; set; }
        public bool IsSuperAdmin { get; set; } // keep false for normal users
        public bool IsActive { get; set; } = true;
    }
}

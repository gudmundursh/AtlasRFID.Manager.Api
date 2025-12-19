using System.ComponentModel.DataAnnotations;

namespace AtlasRFID.Manager.Api.Dtos
{
    public class LoginRequest
    {
        [Required]
        public string UserNameOrEmail { get; set; }

        [Required]
        public string Password { get; set; }
    }
}

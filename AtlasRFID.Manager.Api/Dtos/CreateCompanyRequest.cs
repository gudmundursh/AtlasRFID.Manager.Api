using System.ComponentModel.DataAnnotations;

namespace AtlasRFID.Manager.Api.Dtos
{
    public class CreateCompanyRequest
    {
        [Required]
        [StringLength(50)]
        public string Code { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }
    }
}

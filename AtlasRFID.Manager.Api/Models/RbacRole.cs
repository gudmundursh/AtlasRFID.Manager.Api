namespace AtlasRFID.Manager.Api.Models
{
    public class RbacRole
    {
        public Guid Id { get; set; }
        public Guid? CompanyId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsSystemRole { get; set; }
        public bool IsActive { get; set; }
    }
}

namespace AtlasRFID.Manager.Api.Dtos
{
    public class CreateRoleRequest
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
    }
}

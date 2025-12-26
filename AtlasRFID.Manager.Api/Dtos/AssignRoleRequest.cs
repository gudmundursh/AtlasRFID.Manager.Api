namespace AtlasRFID.Manager.Api.Dtos
{
    public class AssignRoleRequest
    {
        public Guid RoleId { get; set; }
        public string? ScopeType { get; set; }
        public Guid? ScopeId { get; set; }
    }
}

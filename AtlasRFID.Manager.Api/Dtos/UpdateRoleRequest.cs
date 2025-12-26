namespace AtlasRFID.Manager.Api.Dtos
{
    public class UpdateRoleRequest
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public List<string>? PermissionCodes { get; set; } // optional shortcut (we'll resolve to IDs)
    }
}

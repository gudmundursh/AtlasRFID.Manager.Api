namespace AtlasRFID.Manager.Api.Dtos
{
    public class SetRolePermissionsRequest
    {
        public List<string> PermissionCodes { get; set; } = new List<string>();
    }
}

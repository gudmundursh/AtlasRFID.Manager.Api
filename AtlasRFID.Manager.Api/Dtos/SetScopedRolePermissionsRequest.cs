using System.Collections.Generic;

namespace AtlasRFID.Manager.Api.Dtos
{
    public class SetScopedRolePermissionsRequest
    {
        public List<string> Allow { get; set; } = new List<string>();
        public List<string> Deny { get; set; } = new List<string>();
    }
}

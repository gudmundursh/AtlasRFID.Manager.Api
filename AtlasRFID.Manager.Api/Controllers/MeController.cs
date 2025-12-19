using AtlasRFID.Manager.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AtlasRFID.Manager.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/me")]
    public class MeController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue("sub"); // JWT "sub"

            var companyId = User.FindFirstValue("company_id"); // might be null
            var isSuperAdmin = string.Equals(User.FindFirstValue("is_super_admin"), "true", StringComparison.OrdinalIgnoreCase);
            var isCompanyAdmin = string.Equals(User.FindFirstValue("is_company_admin"), "true", StringComparison.OrdinalIgnoreCase);

            return Ok(new MeResponse
            {
                UserId = userId,
                CompanyId = companyId,
                IsSuperAdmin = isSuperAdmin,
                IsCompanyAdmin = isCompanyAdmin
            });
        }
    }
}

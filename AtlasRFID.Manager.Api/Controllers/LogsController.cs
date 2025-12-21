using AtlasRFID.Manager.Api.Repositories;
using AtlasRFID.Manager.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AtlasRFID.Manager.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/logs")]
    public class LogsController : ControllerBase
    {
        private readonly LogRepository _repo;
        private readonly ITenantProvider _tenant;

        public LogsController(LogRepository repo, ITenantProvider tenant)
        {
            _repo = repo;
            _tenant = tenant;
        }

        [HttpGet]
        public async Task<IActionResult> Search(
            [FromQuery] string? category,
            [FromQuery] string? level,
            [FromQuery] string? q,
            [FromQuery] DateTime? fromUtc,
            [FromQuery] DateTime? toUtc,
            [FromQuery] int take = 200)
        {
            var companyId = _tenant.GetCompanyId();
            var rows = await _repo.SearchTenantAsync(companyId, category, level, q, fromUtc, toUtc, take);
            return Ok(rows);
        }
    }
}

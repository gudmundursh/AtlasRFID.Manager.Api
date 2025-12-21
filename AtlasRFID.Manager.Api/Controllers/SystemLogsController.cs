using AtlasRFID.Manager.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AtlasRFID.Manager.Api.Controllers
{
    [Authorize(Policy = "SuperAdminOnly")]
    [ApiController]
    [Route("api/system/logs")]
    public class SystemLogsController : ControllerBase
    {
        private readonly LogRepository _repo;

        public SystemLogsController(LogRepository repo)
        {
            _repo = repo;
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
            var rows = await _repo.SearchSystemAsync(category, level, q, fromUtc, toUtc, take);
            return Ok(rows);
        }
    }
}

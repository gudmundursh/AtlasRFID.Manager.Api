using AtlasRFID.Manager.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AtlasRFID.Manager.Api.Controllers
{
    [Authorize(Policy = "SuperAdminOnly")]
    [ApiController]
    [Route("api/system/companies")]
    public class SystemCompaniesController : ControllerBase
    {
        private readonly CompanyRepository _repo;

        public SystemCompaniesController(CompanyRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var companies = await _repo.GetAllSystemAsync();
            return Ok(companies);
        }
    }
}

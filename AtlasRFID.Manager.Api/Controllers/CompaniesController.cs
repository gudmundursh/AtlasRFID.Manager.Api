using Microsoft.AspNetCore.Mvc;
using AtlasRFID.Manager.Api.Repositories;

namespace AtlasRFID.Manager.Api.Controllers
{
    [ApiController]
    [Route("api/companies")]
    public class CompaniesController : ControllerBase
    {
        private readonly CompanyRepository _repository;

        public CompaniesController(CompanyRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var companies = await _repository.GetAllAsync();
            return Ok(companies);
        }
    }
}

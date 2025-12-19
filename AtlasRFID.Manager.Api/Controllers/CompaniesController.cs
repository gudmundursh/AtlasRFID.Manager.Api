using Microsoft.AspNetCore.Mvc;
using AtlasRFID.Manager.Api.Dtos;
using AtlasRFID.Manager.Api.Models;
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
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCompanyRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var id = await _repository.CreateAsync(request.Code, request.Name);

            var created = await _repository.GetByIdAsync(id);
            if (created == null)
                return StatusCode(500, new { error = "create_company_failed" });

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var company = await _repository.GetByIdAsync(id);
            if (company == null) return NotFound();
            return Ok(company);
        }

    }
}

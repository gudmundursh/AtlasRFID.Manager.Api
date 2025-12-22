using AtlasRFID.Manager.Api.Dtos;
using AtlasRFID.Manager.Api.Repositories;
using AtlasRFID.Manager.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AtlasRFID.Manager.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/companies")]
    public class CompaniesController : ControllerBase
    {
        private readonly CompanyRepository _repository;
        private readonly IAuditLogger _audit;
        private readonly ICorrelationIdProvider _corr;

        public CompaniesController(
            CompanyRepository repository,
            IAuditLogger audit,
            ICorrelationIdProvider corr)
        {
            _repository = repository;
            _audit = audit;
            _corr = corr;
        }

        [Authorize(Policy = "CompanyAdminOnly")]
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

            // --- audit log ---
            Guid? userId = null;
            var userIdStr = User.FindFirst("user_id")?.Value;
            if (Guid.TryParse(userIdStr, out var parsedUserId))
                userId = parsedUserId;

            await _audit.WriteAsync(
                companyId: null,
                userId: userId,
                action: "Create",
                entityType: "Company",
                entityId: created.Id,
                before: null,
                after: created,
                message: $"Created company '{created.Name}'",
                correlationId: _corr.Get()
            );

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
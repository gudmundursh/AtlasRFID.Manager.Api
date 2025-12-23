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
    public class CompaniesController : AppControllerBase
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

            await AuditAsync(
                _audit, _corr,
                companyId: null,
                action: "Create",
                entityType: "Company",
                entityId: created.Id,
                before: null,
                after: created,
                message: $"Created company '{created.Name}'"
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

        [Authorize(Policy = "CompanyAdminOnly")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCompanyRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var before = await _repository.GetByIdAsync(id);
            if (before == null) return NotFound();

            await _repository.UpdateAsync(id, request.Name, request.IsActive);

            var after = await _repository.GetByIdAsync(id);
            if (after == null) return StatusCode(500, new { error = "update_company_failed" });

            await AuditAsync(
                _audit, _corr,
                companyId: null,
                action: "Update",
                entityType: "Company",
                entityId: id,
                before: before,
                after: after,
                message: $"Updated company '{before.Name}' -> '{after.Name}', Active={after.IsActive}"
            );

            return Ok(after);
        }

        [Authorize(Policy = "CompanyAdminOnly")]
        [HttpPost("{id:guid}/deactivate")]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            var before = await _repository.GetByIdAsync(id);
            if (before == null) return NotFound();

            if (!before.IsActive)
                return Ok(before); // already inactive

            await _repository.SetActiveAsync(id, false);

            var after = await _repository.GetByIdAsync(id);
            if (after == null) return StatusCode(500, new { error = "deactivate_company_failed" });

            await AuditAsync(
                _audit, _corr,
                companyId: null,
                action: "Deactivate",
                entityType: "Company",
                entityId: id,
                before: before,
                after: after,
                message: $"Deactivated company '{after.Name}'"
            );

            return Ok(after);
        }


    }
}
using AtlasRFID.Manager.Api.Dtos;
using AtlasRFID.Manager.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AtlasRFID.Manager.Api.Controllers
{
    [Authorize(Policy = "SuperAdminOnly")]
    [ApiController]
    [Route("api/system/users")]
    public class SystemUsersController : ControllerBase
    {
        private readonly UserRepository _users;

        public SystemUsersController(UserRepository users)
        {
            _users = users;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            if (!request.IsSuperAdmin && string.IsNullOrWhiteSpace(request.CompanyId))
                return BadRequest(new { error = "company_id_required_for_non_superadmin" });

            if (!string.IsNullOrWhiteSpace(request.CompanyId) && !Guid.TryParse(request.CompanyId, out _))
                return BadRequest(new { error = "invalid_company_id" });

            if (await _users.UserNameOrEmailExistsAsync(request.UserName, request.Email))
                return Conflict(new { error = "username_or_email_exists" });

            var createdBy =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!Guid.TryParse(createdBy, out var createdByUserId))
                return Unauthorized(new { error = "missing_user_id_claim" });


            Guid? companyId = null;
            if (!string.IsNullOrWhiteSpace(request.CompanyId))
                companyId = Guid.Parse(request.CompanyId);

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var newId = await _users.CreateAsync(
                companyId,
                request.UserName,
                request.Email,
                request.DisplayName,
                passwordHash,
                request.IsSuperAdmin,
                request.IsCompanyAdmin,
                request.IsActive,
                createdByUserId);

            return Ok(new { id = newId });
        }
    }
}

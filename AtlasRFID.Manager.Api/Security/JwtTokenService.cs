using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AtlasRFID.Manager.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace AtlasRFID.Manager.Api.Security
{
    public class JwtTokenService
    {
        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public (string token, int expiresInSeconds) CreateAccessToken(UserAuthRow user)
        {
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            var key = _configuration["Jwt:SigningKey"];
            var minutes = int.Parse(_configuration["Jwt:AccessTokenMinutes"] ?? "60");

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("is_super_admin", user.IsSuperAdmin ? "true" : "false"),
                new Claim("is_company_admin", user.IsCompanyAdmin ? "true" : "false"),
            };

            // company_id claim: only present if not null
            if (user.CompanyId.HasValue)
                claims.Add(new Claim("company_id", user.CompanyId.Value.ToString()));

            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256
            );

            var expires = DateTime.UtcNow.AddMinutes(minutes);

            var jwt = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            var token = new JwtSecurityTokenHandler().WriteToken(jwt);
            return (token, (int)TimeSpan.FromMinutes(minutes).TotalSeconds);
        }
    }
}

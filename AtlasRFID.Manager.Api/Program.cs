using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using AtlasRFID.Manager.Api.Data;
using AtlasRFID.Manager.Api.Repositories;
using AtlasRFID.Manager.Api.Security;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddSingleton<DbConnectionFactory>();
builder.Services.AddScoped<CompanyRepository>();
//builder.Services.AddScoped<ITenantProvider, SingleTenantProvider>();
builder.Services.AddScoped<ITenantProvider, JwtOrSingleTenantProvider>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<LogRepository>();
builder.Services.AddScoped<RbacRepository>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<AtlasRFID.Manager.Api.Services.IAuditLogger, AtlasRFID.Manager.Api.Services.AuditLogger>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AtlasRFID.Manager.Api.Services.ICorrelationIdProvider, AtlasRFID.Manager.Api.Services.CorrelationIdProvider>();
builder.Services.AddHttpContextAccessor();





var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var jwtKey = builder.Configuration["Jwt:SigningKey"];

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminOnly", policy =>
        policy.RequireClaim("is_super_admin", "true"));

    options.AddPolicy("CompanyAdminOnly", policy =>
        policy.RequireClaim("is_company_admin", "true"));
});



var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

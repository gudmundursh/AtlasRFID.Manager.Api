using AtlasRFID.Manager.Api.Data;
using AtlasRFID.Manager.Api.Repositories;
using AtlasRFID.Manager.Api.Security;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddSingleton<DbConnectionFactory>();
builder.Services.AddScoped<CompanyRepository>();
builder.Services.AddScoped<ITenantProvider, SingleTenantProvider>();


var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

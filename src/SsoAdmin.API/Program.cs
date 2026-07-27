using Microsoft.EntityFrameworkCore;
using SsoAdmin.API.Auth;
using SsoAdmin.Application;
using SsoAdmin.Data;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Cadena de conexión externalizada (Principio II).
string connectionString = builder.Configuration.GetConnectionString("Default") ?? string.Empty;
builder.Services.AddSsoAdminData(connectionString);
builder.Services.AddSsoAdminApplication();

// Clave de API del endpoint SSO, externalizada (FR-016 / Principio II).
builder.Services.Configure<SsoApiKeyOptions>(
    builder.Configuration.GetSection(SsoApiKeyOptions.SectionName));

builder.Services.AddAuthentication(ApiKeyAuthenticationOptions.SchemeName)
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationOptions.SchemeName, _ => { });
builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

WebApplication app = builder.Build();

// Bajo un proveedor relacional no-SqlServer (SQLite en tests) se crea el esquema; en
// producción el esquema de SQL Server lo gestionan SsoAdmin.Web y la CLI de EF Core.
using (IServiceScope scope = app.Services.CreateScope())
{
    SsoAdminDbContext context = scope.ServiceProvider.GetRequiredService<SsoAdminDbContext>();
    if (context.Database.IsRelational() && !context.Database.IsSqlServer())
    {
        await context.Database.EnsureCreatedAsync();
    }
}

// Manejo global de errores: toda excepción no controlada se traduce a 500 sin filtrar
// detalles internos (FR-009). Aplica en todos los ambientes por ser una API de máquina.
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(new { error = "internal_error" });
}));

// Documento OpenAPI del contrato SSO (research.md #9). Expuesto en todos los ambientes por
// ser una API de máquina cuyo contrato deben poder inspeccionar el SSO externo y SI.
app.MapOpenApi();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

/// <summary>Punto de entrada de la API; expuesto como parcial para los tests de integración.</summary>
public partial class Program
{
}

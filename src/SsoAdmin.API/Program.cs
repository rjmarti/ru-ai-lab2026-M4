using Microsoft.EntityFrameworkCore;
using SsoAdmin.Data;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Cadena de conexión externalizada (Principio II).
string connectionString = builder.Configuration.GetConnectionString("Default") ?? string.Empty;
builder.Services.AddSsoAdminData(connectionString);

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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

/// <summary>Punto de entrada de la API; expuesto como parcial para los tests de integración.</summary>
public partial class Program
{
}

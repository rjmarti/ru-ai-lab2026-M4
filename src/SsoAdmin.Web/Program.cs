using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SsoAdmin.Data;
using SsoAdmin.Data.Seed;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Cadena de conexión externalizada (Principio II).
string connectionString = builder.Configuration.GetConnectionString("Default") ?? string.Empty;
builder.Services.AddSsoAdminData(connectionString);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/api/auth/logout";
        options.AccessDeniedPath = "/Login";
        options.Cookie.Name = "SsoAdmin.Auth";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

builder.Services.AddRazorPages();
builder.Services.AddControllers();

WebApplication app = builder.Build();

// Inicialización de base de datos y seed de la cuenta admin (FR-007).
await InicializarBaseDeDatosAsync(app);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapControllers();

app.Run();

static async Task InicializarBaseDeDatosAsync(WebApplication app)
{
    using IServiceScope scope = app.Services.CreateScope();
    SsoAdminDbContext context = scope.ServiceProvider.GetRequiredService<SsoAdminDbContext>();

    if (context.Database.IsSqlServer())
    {
        await context.Database.MigrateAsync();
    }
    else
    {
        await context.Database.EnsureCreatedAsync();
    }

    LoginSISeeder seeder = scope.ServiceProvider.GetRequiredService<LoginSISeeder>();
    await seeder.SeedAsync();
}

/// <summary>Punto de entrada de la app Web; expuesto como parcial para los tests de integración.</summary>
public partial class Program
{
}

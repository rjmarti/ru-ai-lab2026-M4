using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SsoAdmin.Data.Repositories;
using SsoAdmin.Data.Seed;
using SsoAdmin.Models;

namespace SsoAdmin.Data;

/// <summary>Extensiones de registro de servicios de la capa de datos.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra el <see cref="SsoAdminDbContext"/> (SQL Server), los repositorios, el
    /// <see cref="PasswordHasher{TUser}"/> y el <see cref="LoginSISeeder"/>. La cadena de
    /// conexión se provee externalizada (Principio II).
    /// </summary>
    public static IServiceCollection AddSsoAdminData(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<SsoAdminDbContext>(options => options.UseSqlServer(connectionString));
        return services.AddSsoAdminDataServices();
    }

    /// <summary>
    /// Registra los repositorios, el <see cref="PasswordHasher{TUser}"/> y el seeder sin
    /// configurar el proveedor de base de datos. Usado por los tests para sustituir el
    /// proveedor por SQLite relacional.
    /// </summary>
    public static IServiceCollection AddSsoAdminDataServices(this IServiceCollection services)
    {
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<ICredencialRepository, CredencialRepository>();
        services.AddScoped<IAplicacionRepository, AplicacionRepository>();
        services.AddScoped<IPermisoAccesoRepository, PermisoAccesoRepository>();
        services.AddScoped<ILoginSIRepository, LoginSIRepository>();
        services.AddSingleton<IPasswordHasher<LoginSI>, PasswordHasher<LoginSI>>();
        services.AddScoped<LoginSISeeder>();
        return services;
    }
}

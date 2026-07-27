using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SsoAdmin.Application.Features.AuthSI;
using SsoAdmin.Application.Features.GestionUsuarios;
using SsoAdmin.Application.Features.VerificarAcceso;

namespace SsoAdmin.Application;

/// <summary>Extensiones de registro de servicios de la capa de aplicación (casos de uso).</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra los handlers y validadores de los casos de uso, y el <see cref="TimeProvider"/>
    /// usado para resolver la fecha actual de forma testeable.
    /// </summary>
    public static IServiceCollection AddSsoAdminApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        // US1 — Verificación de acceso (endpoint SSO).
        services.AddScoped<IValidator<VerificarAccesoRequest>, VerificarAccesoValidator>();
        services.AddScoped<VerificarAccesoHandler>();

        // US2 — Login SI y gestión de usuarios.
        services.AddScoped<IValidator<LoginRequest>, LoginValidator>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<IValidator<CrearUsuarioRequest>, CrearUsuarioValidator>();
        services.AddScoped<IValidator<EditarUsuarioRequest>, EditarUsuarioValidator>();
        services.AddScoped<ListarUsuariosHandler>();
        services.AddScoped<CrearUsuarioHandler>();
        services.AddScoped<EditarUsuarioHandler>();
        services.AddScoped<DarBajaUsuarioHandler>();

        return services;
    }
}

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
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

        return services;
    }
}

using FluentValidation;

namespace SsoAdmin.Application.Features.GestionPermisos;

/// <summary>
/// Valida el otorgamiento de un permiso: usuario y aplicación requeridos y, cuando hay
/// <c>FechaHasta</c>, que <c>FechaDesde &lt;= FechaHasta</c> (edge case de validación).
/// </summary>
public sealed class OtorgarPermisoValidator : AbstractValidator<OtorgarPermisoRequest>
{
    /// <summary>Configura las reglas de validación.</summary>
    public OtorgarPermisoValidator()
    {
        RuleFor(r => r.UsuarioId).NotEmpty();
        RuleFor(r => r.AplicacionId).NotEmpty();
        RuleFor(r => r.FechaHasta)
            .GreaterThanOrEqualTo(r => r.FechaDesde)
            .When(r => r.FechaHasta is not null)
            .WithMessage("La fecha desde no puede ser posterior a la fecha hasta.");
    }
}

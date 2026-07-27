using FluentValidation;

namespace SsoAdmin.Application.Features.VerificarAcceso;

/// <summary>
/// Valida que la solicitud del SSO incluya todos los campos requeridos (FR-009: campos
/// faltantes → 400 Bad Request).
/// </summary>
public sealed class VerificarAccesoValidator : AbstractValidator<VerificarAccesoRequest>
{
    /// <summary>Configura las reglas de validación.</summary>
    public VerificarAccesoValidator()
    {
        RuleFor(r => r.Username).NotEmpty();
        RuleFor(r => r.Emisor).NotEmpty();
        RuleFor(r => r.AplicacionUrl).NotEmpty();
    }
}

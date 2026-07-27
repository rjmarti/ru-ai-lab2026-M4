using FluentValidation;

namespace SsoAdmin.Application.Features.GestionCredenciales;

/// <summary>Valida la creación de una credencial (usuario, username y emisor requeridos).</summary>
public sealed class CrearCredencialValidator : AbstractValidator<CrearCredencialRequest>
{
    /// <summary>Configura las reglas de validación.</summary>
    public CrearCredencialValidator()
    {
        RuleFor(r => r.UsuarioId).NotEmpty();
        RuleFor(r => r.Username).NotEmpty();
        RuleFor(r => r.Emisor).NotEmpty();
    }
}

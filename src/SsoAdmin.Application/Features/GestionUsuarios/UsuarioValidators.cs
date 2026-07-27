using FluentValidation;

namespace SsoAdmin.Application.Features.GestionUsuarios;

/// <summary>Valida la creación de un usuario (nombre no vacío, FR-010).</summary>
public sealed class CrearUsuarioValidator : AbstractValidator<CrearUsuarioRequest>
{
    /// <summary>Configura las reglas de validación.</summary>
    public CrearUsuarioValidator() => RuleFor(r => r.Nombre).NotEmpty();
}

/// <summary>Valida la edición de un usuario (nombre no vacío, FR-010).</summary>
public sealed class EditarUsuarioValidator : AbstractValidator<EditarUsuarioRequest>
{
    /// <summary>Configura las reglas de validación.</summary>
    public EditarUsuarioValidator() => RuleFor(r => r.Nombre).NotEmpty();
}

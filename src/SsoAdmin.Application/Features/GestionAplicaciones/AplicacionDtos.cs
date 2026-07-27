using FluentValidation;

namespace SsoAdmin.Application.Features.GestionAplicaciones;

/// <summary>Ítem de listado de aplicaciones (FR-012).</summary>
/// <param name="Id">Identificador de la aplicación.</param>
/// <param name="Nombre">Nombre de la aplicación.</param>
/// <param name="Url">URL de la aplicación.</param>
public sealed record AplicacionListItem(Guid Id, string Nombre, string Url);

/// <summary>Datos para registrar una aplicación.</summary>
/// <param name="Nombre">Nombre. Requerido.</param>
/// <param name="Url">URL. Requerida, no vacía (FR-003).</param>
public sealed record CrearAplicacionRequest(string Nombre, string Url);

/// <summary>Datos para editar una aplicación.</summary>
/// <param name="Nombre">Nombre. Requerido.</param>
/// <param name="Url">URL. Requerida, no vacía (FR-003).</param>
public sealed record EditarAplicacionRequest(string Nombre, string Url);

/// <summary>Valida la creación de una aplicación (nombre y URL no vacíos, FR-003).</summary>
public sealed class CrearAplicacionValidator : AbstractValidator<CrearAplicacionRequest>
{
    /// <summary>Configura las reglas de validación.</summary>
    public CrearAplicacionValidator()
    {
        RuleFor(r => r.Nombre).NotEmpty();
        RuleFor(r => r.Url).NotEmpty();
    }
}

/// <summary>Valida la edición de una aplicación (nombre y URL no vacíos, FR-003).</summary>
public sealed class EditarAplicacionValidator : AbstractValidator<EditarAplicacionRequest>
{
    /// <summary>Configura las reglas de validación.</summary>
    public EditarAplicacionValidator()
    {
        RuleFor(r => r.Nombre).NotEmpty();
        RuleFor(r => r.Url).NotEmpty();
    }
}

namespace SsoAdmin.Application.Features.GestionUsuarios;

/// <summary>Ítem de listado de usuarios (FR-010).</summary>
/// <param name="Id">Identificador del usuario.</param>
/// <param name="Nombre">Nombre del usuario.</param>
/// <param name="Activo">Estado activo/inactivo.</param>
public sealed record UsuarioListItem(Guid Id, string Nombre, bool Activo);

/// <summary>Datos para crear un usuario.</summary>
/// <param name="Nombre">Nombre del usuario. Requerido.</param>
public sealed record CrearUsuarioRequest(string Nombre);

/// <summary>Datos para editar el nombre de un usuario.</summary>
/// <param name="Nombre">Nuevo nombre del usuario. Requerido.</param>
public sealed record EditarUsuarioRequest(string Nombre);

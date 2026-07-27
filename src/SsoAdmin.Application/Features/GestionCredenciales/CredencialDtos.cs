namespace SsoAdmin.Application.Features.GestionCredenciales;

/// <summary>Ítem de listado de credenciales con su usuario asociado (FR-011).</summary>
/// <param name="Id">Identificador de la credencial.</param>
/// <param name="UsuarioId">Identificador del usuario dueño.</param>
/// <param name="UsuarioNombre">Nombre del usuario dueño.</param>
/// <param name="Username">Nombre de usuario de la credencial.</param>
/// <param name="Emisor">Proveedor de identidad emisor.</param>
public sealed record CredencialListItem(Guid Id, Guid UsuarioId, string UsuarioNombre, string Username, string Emisor);

/// <summary>Datos para crear una credencial.</summary>
/// <param name="UsuarioId">Usuario dueño. Requerido.</param>
/// <param name="Username">Nombre de usuario. Requerido.</param>
/// <param name="Emisor">Proveedor de identidad. Requerido.</param>
public sealed record CrearCredencialRequest(Guid UsuarioId, string Username, string Emisor);

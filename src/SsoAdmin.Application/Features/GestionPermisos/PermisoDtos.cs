namespace SsoAdmin.Application.Features.GestionPermisos;

/// <summary>Ítem de listado de permisos de acceso (FR-004).</summary>
/// <param name="Id">Identificador del permiso.</param>
/// <param name="UsuarioId">Usuario al que aplica.</param>
/// <param name="AplicacionId">Aplicación sobre la que aplica.</param>
/// <param name="FechaDesde">Inicio de vigencia.</param>
/// <param name="FechaHasta">Fin de vigencia; <c>null</c> = indefinido.</param>
public sealed record PermisoListItem(Guid Id, Guid UsuarioId, Guid AplicacionId, DateOnly FechaDesde, DateOnly? FechaHasta);

/// <summary>Datos para otorgar un permiso de acceso.</summary>
/// <param name="UsuarioId">Usuario. Requerido.</param>
/// <param name="AplicacionId">Aplicación. Requerido.</param>
/// <param name="FechaDesde">Inicio de vigencia. Requerido.</param>
/// <param name="FechaHasta">Fin de vigencia. Opcional (indefinido si se omite).</param>
public sealed record OtorgarPermisoRequest(Guid UsuarioId, Guid AplicacionId, DateOnly FechaDesde, DateOnly? FechaHasta);

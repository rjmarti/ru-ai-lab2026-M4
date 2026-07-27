using SsoAdmin.Application.Common;
using SsoAdmin.Data.Repositories;
using SsoAdmin.Models;

namespace SsoAdmin.Application.Features.GestionPermisos;

/// <summary>
/// Otorga un permiso de acceso verificando que el usuario y la aplicación existan y que el
/// período no se solape con uno existente. El chequeo de solapamiento corre dentro de una
/// transacción <c>Serializable</c> en el repositorio (FR-004, research.md #5); el solapamiento
/// se traduce a <c>409 Conflict</c> (US4-AC2).
/// </summary>
public sealed class OtorgarPermisoHandler
{
    private readonly IPermisoAccesoRepository _permisos;
    private readonly IUsuarioRepository _usuarios;
    private readonly IAplicacionRepository _aplicaciones;

    /// <summary>Crea el handler con las dependencias inyectadas.</summary>
    public OtorgarPermisoHandler(
        IPermisoAccesoRepository permisos,
        IUsuarioRepository usuarios,
        IAplicacionRepository aplicaciones)
    {
        _permisos = permisos;
        _usuarios = usuarios;
        _aplicaciones = aplicaciones;
    }

    /// <summary>Otorga el permiso o devuelve el error correspondiente (400/409).</summary>
    public async Task<Result<PermisoListItem>> HandleAsync(OtorgarPermisoRequest request, CancellationToken cancellationToken = default)
    {
        if (await _usuarios.ObtenerAsync(request.UsuarioId, cancellationToken) is null)
        {
            return Result<PermisoListItem>.Validation("El usuario indicado no existe.");
        }

        if (await _aplicaciones.ObtenerAsync(request.AplicacionId, cancellationToken) is null)
        {
            return Result<PermisoListItem>.Validation("La aplicación indicada no existe.");
        }

        PermisoAcceso permiso = new()
        {
            Id = Guid.NewGuid(),
            UsuarioId = request.UsuarioId,
            AplicacionId = request.AplicacionId,
            FechaDesde = request.FechaDesde,
            FechaHasta = request.FechaHasta
        };

        ResultadoOtorgarPermiso resultado = await _permisos.OtorgarAsync(permiso, cancellationToken);
        if (resultado == ResultadoOtorgarPermiso.Solapado)
        {
            return Result<PermisoListItem>.Conflict("El período se solapa con un permiso existente.");
        }

        return Result<PermisoListItem>.Success(
            new PermisoListItem(permiso.Id, permiso.UsuarioId, permiso.AplicacionId, permiso.FechaDesde, permiso.FechaHasta));
    }
}

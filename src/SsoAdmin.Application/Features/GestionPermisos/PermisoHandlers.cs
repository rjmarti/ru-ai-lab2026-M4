using SsoAdmin.Application.Common;
using SsoAdmin.Data.Repositories;
using SsoAdmin.Models;

namespace SsoAdmin.Application.Features.GestionPermisos;

/// <summary>Lista permisos con filtros opcionales por usuario y aplicación (FR-004).</summary>
public sealed class ListarPermisosHandler
{
    private readonly IPermisoAccesoRepository _repository;

    /// <summary>Crea el handler con el repositorio inyectado.</summary>
    public ListarPermisosHandler(IPermisoAccesoRepository repository) => _repository = repository;

    /// <summary>Devuelve el listado de permisos filtrado.</summary>
    public async Task<IReadOnlyList<PermisoListItem>> HandleAsync(Guid? usuarioId, Guid? aplicacionId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PermisoAcceso> permisos = await _repository.ListarAsync(usuarioId, aplicacionId, cancellationToken);
        return permisos
            .Select(p => new PermisoListItem(p.Id, p.UsuarioId, p.AplicacionId, p.FechaDesde, p.FechaHasta))
            .ToList();
    }
}

/// <summary>
/// Revoca un permiso fijando su <c>FechaHasta</c> en la fecha actual (FR-005). Es idempotente:
/// revocar un permiso ya vencido no produce error ni altera su fecha.
/// </summary>
public sealed class RevocarPermisoHandler
{
    private readonly IPermisoAccesoRepository _repository;
    private readonly TimeProvider _timeProvider;

    /// <summary>Crea el handler con las dependencias inyectadas.</summary>
    public RevocarPermisoHandler(IPermisoAccesoRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    /// <summary>Revoca el permiso; devuelve 404 si no existe.</summary>
    public async Task<Result<bool>> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        PermisoAcceso? permiso = await _repository.ObtenerAsync(id, cancellationToken);
        if (permiso is null)
        {
            return Result<bool>.NotFound("El permiso no existe.");
        }

        DateOnly hoy = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        if (permiso.FechaHasta is null || permiso.FechaHasta > hoy)
        {
            permiso.FechaHasta = hoy;
            await _repository.GuardarCambiosAsync(cancellationToken);
        }

        return Result<bool>.Success(true);
    }
}

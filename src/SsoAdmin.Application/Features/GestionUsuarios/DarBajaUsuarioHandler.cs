using SsoAdmin.Application.Common;
using SsoAdmin.Data.Repositories;
using SsoAdmin.Models;

namespace SsoAdmin.Application.Features.GestionUsuarios;

/// <summary>
/// Da de baja lógica a un usuario y, en la misma operación, caduca todos sus permisos
/// activos fijando <c>FechaHasta = hoy</c> (FR-006/FR-015). Es idempotente: dar de baja a un
/// usuario ya inactivo no produce error.
/// </summary>
public sealed class DarBajaUsuarioHandler
{
    private readonly IUsuarioRepository _repository;
    private readonly TimeProvider _timeProvider;

    /// <summary>Crea el handler con las dependencias inyectadas.</summary>
    public DarBajaUsuarioHandler(IUsuarioRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    /// <summary>Ejecuta la baja lógica en cascada; devuelve 404 si el usuario no existe.</summary>
    public async Task<Result<bool>> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Usuario? usuario = await _repository.ObtenerConPermisosAsync(id, cancellationToken);
        if (usuario is null)
        {
            return Result<bool>.NotFound("El usuario no existe.");
        }

        DateOnly hoy = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        usuario.Activo = false;

        foreach (PermisoAcceso permiso in usuario.Permisos)
        {
            if (permiso.FechaHasta is null || permiso.FechaHasta >= hoy)
            {
                permiso.FechaHasta = hoy;
            }
        }

        await _repository.GuardarCambiosAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}

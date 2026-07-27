using SsoAdmin.Application.Common;
using SsoAdmin.Data.Repositories;
using SsoAdmin.Models;

namespace SsoAdmin.Application.Features.GestionAplicaciones;

/// <summary>Lista todas las aplicaciones registradas (FR-012).</summary>
public sealed class ListarAplicacionesHandler
{
    private readonly IAplicacionRepository _repository;

    /// <summary>Crea el handler con el repositorio inyectado.</summary>
    public ListarAplicacionesHandler(IAplicacionRepository repository) => _repository = repository;

    /// <summary>Devuelve el listado de aplicaciones.</summary>
    public async Task<IReadOnlyList<AplicacionListItem>> HandleAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Aplicacion> aplicaciones = await _repository.ListarAsync(cancellationToken);
        return aplicaciones.Select(a => new AplicacionListItem(a.Id, a.Nombre, a.Url)).ToList();
    }
}

/// <summary>Registra una nueva aplicación (FR-003/FR-012).</summary>
public sealed class CrearAplicacionHandler
{
    private readonly IAplicacionRepository _repository;

    /// <summary>Crea el handler con el repositorio inyectado.</summary>
    public CrearAplicacionHandler(IAplicacionRepository repository) => _repository = repository;

    /// <summary>Crea la aplicación y devuelve su representación de listado.</summary>
    public async Task<Result<AplicacionListItem>> HandleAsync(CrearAplicacionRequest request, CancellationToken cancellationToken = default)
    {
        Aplicacion aplicacion = new() { Id = Guid.NewGuid(), Nombre = request.Nombre, Url = request.Url };
        await _repository.AgregarAsync(aplicacion, cancellationToken);
        await _repository.GuardarCambiosAsync(cancellationToken);
        return Result<AplicacionListItem>.Success(new AplicacionListItem(aplicacion.Id, aplicacion.Nombre, aplicacion.Url));
    }
}

/// <summary>Edita nombre y URL de una aplicación existente (FR-012).</summary>
public sealed class EditarAplicacionHandler
{
    private readonly IAplicacionRepository _repository;

    /// <summary>Crea el handler con el repositorio inyectado.</summary>
    public EditarAplicacionHandler(IAplicacionRepository repository) => _repository = repository;

    /// <summary>Actualiza la aplicación; devuelve 404 si no existe.</summary>
    public async Task<Result<AplicacionListItem>> HandleAsync(Guid id, EditarAplicacionRequest request, CancellationToken cancellationToken = default)
    {
        Aplicacion? aplicacion = await _repository.ObtenerAsync(id, cancellationToken);
        if (aplicacion is null)
        {
            return Result<AplicacionListItem>.NotFound("La aplicación no existe.");
        }

        aplicacion.Nombre = request.Nombre;
        aplicacion.Url = request.Url;
        await _repository.GuardarCambiosAsync(cancellationToken);
        return Result<AplicacionListItem>.Success(new AplicacionListItem(aplicacion.Id, aplicacion.Nombre, aplicacion.Url));
    }
}

/// <summary>Elimina físicamente una aplicación, aun con permisos activos (FR-012, edge case).</summary>
public sealed class EliminarAplicacionHandler
{
    private readonly IAplicacionRepository _repository;

    /// <summary>Crea el handler con el repositorio inyectado.</summary>
    public EliminarAplicacionHandler(IAplicacionRepository repository) => _repository = repository;

    /// <summary>Elimina la aplicación; devuelve 404 si no existe.</summary>
    public async Task<Result<bool>> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        bool eliminada = await _repository.EliminarAsync(id, cancellationToken);
        return eliminada
            ? Result<bool>.Success(true)
            : Result<bool>.NotFound("La aplicación no existe.");
    }
}

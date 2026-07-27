using SsoAdmin.Application.Common;
using SsoAdmin.Data.Repositories;
using SsoAdmin.Models;

namespace SsoAdmin.Application.Features.GestionCredenciales;

/// <summary>Lista todas las credenciales con su usuario asociado (FR-011).</summary>
public sealed class ListarCredencialesHandler
{
    private readonly ICredencialRepository _repository;

    /// <summary>Crea el handler con el repositorio inyectado.</summary>
    public ListarCredencialesHandler(ICredencialRepository repository) => _repository = repository;

    /// <summary>Devuelve el listado de credenciales.</summary>
    public async Task<IReadOnlyList<CredencialListItem>> HandleAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Credencial> credenciales = await _repository.ListarConUsuarioAsync(cancellationToken);
        return credenciales
            .Select(c => new CredencialListItem(c.Id, c.UsuarioId, c.Usuario?.Nombre ?? string.Empty, c.Username, c.Emisor))
            .ToList();
    }
}

/// <summary>
/// Crea una credencial validando que el usuario exista y que la combinación
/// <c>(Username, Emisor)</c> sea única; una violación del índice único se traduce a
/// <c>400</c> (FR-001/FR-002, US3-AC1/AC2).
/// </summary>
public sealed class CrearCredencialHandler
{
    private readonly ICredencialRepository _credenciales;
    private readonly IUsuarioRepository _usuarios;

    /// <summary>Crea el handler con las dependencias inyectadas.</summary>
    public CrearCredencialHandler(ICredencialRepository credenciales, IUsuarioRepository usuarios)
    {
        _credenciales = credenciales;
        _usuarios = usuarios;
    }

    /// <summary>Crea la credencial o devuelve un error 400 si el usuario no existe o hay duplicado.</summary>
    public async Task<Result<CredencialListItem>> HandleAsync(CrearCredencialRequest request, CancellationToken cancellationToken = default)
    {
        Usuario? usuario = await _usuarios.ObtenerAsync(request.UsuarioId, cancellationToken);
        if (usuario is null)
        {
            return Result<CredencialListItem>.Validation("El usuario indicado no existe.");
        }

        Credencial credencial = new()
        {
            Id = Guid.NewGuid(),
            UsuarioId = request.UsuarioId,
            Username = request.Username,
            Emisor = request.Emisor
        };

        bool creada = await _credenciales.IntentarCrearAsync(credencial, cancellationToken);
        if (!creada)
        {
            return Result<CredencialListItem>.Validation(
                "La combinación de username y emisor ya está en uso.");
        }

        return Result<CredencialListItem>.Success(
            new CredencialListItem(credencial.Id, usuario.Id, usuario.Nombre, credencial.Username, credencial.Emisor));
    }
}

/// <summary>Elimina físicamente una credencial (FR-011).</summary>
public sealed class EliminarCredencialHandler
{
    private readonly ICredencialRepository _repository;

    /// <summary>Crea el handler con el repositorio inyectado.</summary>
    public EliminarCredencialHandler(ICredencialRepository repository) => _repository = repository;

    /// <summary>Elimina la credencial; devuelve 404 si no existe.</summary>
    public async Task<Result<bool>> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        bool eliminada = await _repository.EliminarAsync(id, cancellationToken);
        return eliminada
            ? Result<bool>.Success(true)
            : Result<bool>.NotFound("La credencial no existe.");
    }
}

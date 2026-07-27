using SsoAdmin.Application.Common;
using SsoAdmin.Data.Repositories;
using SsoAdmin.Models;

namespace SsoAdmin.Application.Features.GestionUsuarios;

/// <summary>Lista todos los usuarios con su estado (FR-010).</summary>
public sealed class ListarUsuariosHandler
{
    private readonly IUsuarioRepository _repository;

    /// <summary>Crea el handler con el repositorio inyectado.</summary>
    public ListarUsuariosHandler(IUsuarioRepository repository) => _repository = repository;

    /// <summary>Devuelve el listado de usuarios.</summary>
    public async Task<IReadOnlyList<UsuarioListItem>> HandleAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Usuario> usuarios = await _repository.ListarAsync(cancellationToken);
        return usuarios.Select(u => new UsuarioListItem(u.Id, u.Nombre, u.Activo)).ToList();
    }
}

/// <summary>Crea un nuevo usuario (FR-010).</summary>
public sealed class CrearUsuarioHandler
{
    private readonly IUsuarioRepository _repository;

    /// <summary>Crea el handler con el repositorio inyectado.</summary>
    public CrearUsuarioHandler(IUsuarioRepository repository) => _repository = repository;

    /// <summary>Crea el usuario y devuelve su representación de listado.</summary>
    public async Task<Result<UsuarioListItem>> HandleAsync(CrearUsuarioRequest request, CancellationToken cancellationToken = default)
    {
        Usuario usuario = new() { Id = Guid.NewGuid(), Nombre = request.Nombre, Activo = true };
        await _repository.AgregarAsync(usuario, cancellationToken);
        await _repository.GuardarCambiosAsync(cancellationToken);
        return Result<UsuarioListItem>.Success(new UsuarioListItem(usuario.Id, usuario.Nombre, usuario.Activo));
    }
}

/// <summary>Edita el nombre de un usuario existente (FR-010).</summary>
public sealed class EditarUsuarioHandler
{
    private readonly IUsuarioRepository _repository;

    /// <summary>Crea el handler con el repositorio inyectado.</summary>
    public EditarUsuarioHandler(IUsuarioRepository repository) => _repository = repository;

    /// <summary>Actualiza el nombre; devuelve 404 si el usuario no existe.</summary>
    public async Task<Result<UsuarioListItem>> HandleAsync(Guid id, EditarUsuarioRequest request, CancellationToken cancellationToken = default)
    {
        Usuario? usuario = await _repository.ObtenerAsync(id, cancellationToken);
        if (usuario is null)
        {
            return Result<UsuarioListItem>.NotFound("El usuario no existe.");
        }

        usuario.Nombre = request.Nombre;
        await _repository.GuardarCambiosAsync(cancellationToken);
        return Result<UsuarioListItem>.Success(new UsuarioListItem(usuario.Id, usuario.Nombre, usuario.Activo));
    }
}

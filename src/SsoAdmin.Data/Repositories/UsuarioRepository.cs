using Microsoft.EntityFrameworkCore;
using SsoAdmin.Models;

namespace SsoAdmin.Data.Repositories;

/// <summary>Acceso a datos de <see cref="Usuario"/>.</summary>
public interface IUsuarioRepository
{
    /// <summary>Lista todos los usuarios con su estado activo/inactivo.</summary>
    Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancellationToken = default);

    /// <summary>Obtiene un usuario por su identificador, o <c>null</c> si no existe.</summary>
    Task<Usuario?> ObtenerAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Obtiene un usuario junto con sus permisos, o <c>null</c> si no existe.</summary>
    Task<Usuario?> ObtenerConPermisosAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Agrega un nuevo usuario al contexto (no persiste hasta guardar cambios).</summary>
    Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default);

    /// <summary>Persiste los cambios pendientes en la unidad de trabajo.</summary>
    Task GuardarCambiosAsync(CancellationToken cancellationToken = default);
}

/// <summary>Implementación EF Core de <see cref="IUsuarioRepository"/>.</summary>
public class UsuarioRepository : IUsuarioRepository
{
    private readonly SsoAdminDbContext _context;

    /// <summary>Crea el repositorio con el contexto inyectado.</summary>
    public UsuarioRepository(SsoAdminDbContext context) => _context = context;

    /// <inheritdoc />
    public async Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancellationToken = default) =>
        await _context.Usuarios.AsNoTracking().OrderBy(u => u.Nombre).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<Usuario?> ObtenerAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<Usuario?> ObtenerConPermisosAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Usuarios.Include(u => u.Permisos)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default) =>
        await _context.Usuarios.AddAsync(usuario, cancellationToken);

    /// <inheritdoc />
    public async Task GuardarCambiosAsync(CancellationToken cancellationToken = default) =>
        await _context.SaveChangesAsync(cancellationToken);
}

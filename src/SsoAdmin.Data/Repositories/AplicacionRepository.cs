using Microsoft.EntityFrameworkCore;
using SsoAdmin.Models;

namespace SsoAdmin.Data.Repositories;

/// <summary>Acceso a datos de <see cref="Aplicacion"/>.</summary>
public interface IAplicacionRepository
{
    /// <summary>Lista todas las aplicaciones registradas.</summary>
    Task<IReadOnlyList<Aplicacion>> ListarAsync(CancellationToken cancellationToken = default);

    /// <summary>Obtiene una aplicación por su identificador, o <c>null</c> si no existe.</summary>
    Task<Aplicacion?> ObtenerAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Obtiene una aplicación por su URL, o <c>null</c> si no existe. Usado por el SSO.</summary>
    Task<Aplicacion?> ObtenerPorUrlAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>Agrega una nueva aplicación al contexto (no persiste hasta guardar cambios).</summary>
    Task AgregarAsync(Aplicacion aplicacion, CancellationToken cancellationToken = default);

    /// <summary>Elimina físicamente una aplicación. Devuelve <c>false</c> si no existe.</summary>
    Task<bool> EliminarAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persiste los cambios pendientes en la unidad de trabajo.</summary>
    Task GuardarCambiosAsync(CancellationToken cancellationToken = default);
}

/// <summary>Implementación EF Core de <see cref="IAplicacionRepository"/>.</summary>
public class AplicacionRepository : IAplicacionRepository
{
    private readonly SsoAdminDbContext _context;

    /// <summary>Crea el repositorio con el contexto inyectado.</summary>
    public AplicacionRepository(SsoAdminDbContext context) => _context = context;

    /// <inheritdoc />
    public async Task<IReadOnlyList<Aplicacion>> ListarAsync(CancellationToken cancellationToken = default) =>
        await _context.Aplicaciones.AsNoTracking().OrderBy(a => a.Nombre).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<Aplicacion?> ObtenerAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Aplicaciones.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<Aplicacion?> ObtenerPorUrlAsync(string url, CancellationToken cancellationToken = default) =>
        await _context.Aplicaciones.AsNoTracking().FirstOrDefaultAsync(a => a.Url == url, cancellationToken);

    /// <inheritdoc />
    public async Task AgregarAsync(Aplicacion aplicacion, CancellationToken cancellationToken = default) =>
        await _context.Aplicaciones.AddAsync(aplicacion, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> EliminarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Aplicacion? aplicacion = await _context.Aplicaciones.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (aplicacion is null)
        {
            return false;
        }

        _context.Aplicaciones.Remove(aplicacion);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync(CancellationToken cancellationToken = default) =>
        await _context.SaveChangesAsync(cancellationToken);
}

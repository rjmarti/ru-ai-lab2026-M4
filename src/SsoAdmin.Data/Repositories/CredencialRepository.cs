using Microsoft.EntityFrameworkCore;
using SsoAdmin.Models;

namespace SsoAdmin.Data.Repositories;

/// <summary>Acceso a datos de <see cref="Credencial"/>.</summary>
public interface ICredencialRepository
{
    /// <summary>Lista todas las credenciales incluyendo el usuario asociado.</summary>
    Task<IReadOnlyList<Credencial>> ListarConUsuarioAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene la credencial identificada por <paramref name="username"/> + <paramref name="emisor"/>
    /// junto con su usuario, o <c>null</c> si no existe. Usado por el endpoint de verificación SSO.
    /// </summary>
    Task<Credencial?> ObtenerPorUsernameEmisorAsync(string username, string emisor, CancellationToken cancellationToken = default);

    /// <summary>
    /// Intenta crear la credencial. Devuelve <c>true</c> si se creó; <c>false</c> si la combinación
    /// <c>(Username, Emisor)</c> ya existe (violación del índice único, incluso bajo concurrencia).
    /// </summary>
    Task<bool> IntentarCrearAsync(Credencial credencial, CancellationToken cancellationToken = default);

    /// <summary>Elimina físicamente una credencial. Devuelve <c>false</c> si no existe.</summary>
    Task<bool> EliminarAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>Implementación EF Core de <see cref="ICredencialRepository"/>.</summary>
public class CredencialRepository : ICredencialRepository
{
    private readonly SsoAdminDbContext _context;

    /// <summary>Crea el repositorio con el contexto inyectado.</summary>
    public CredencialRepository(SsoAdminDbContext context) => _context = context;

    /// <inheritdoc />
    public async Task<IReadOnlyList<Credencial>> ListarConUsuarioAsync(CancellationToken cancellationToken = default) =>
        await _context.Credenciales.AsNoTracking()
            .Include(c => c.Usuario)
            .OrderBy(c => c.Emisor).ThenBy(c => c.Username)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<Credencial?> ObtenerPorUsernameEmisorAsync(string username, string emisor, CancellationToken cancellationToken = default) =>
        await _context.Credenciales.AsNoTracking()
            .Include(c => c.Usuario)
            .FirstOrDefaultAsync(c => c.Username == username && c.Emisor == emisor, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> IntentarCrearAsync(Credencial credencial, CancellationToken cancellationToken = default)
    {
        await _context.Credenciales.AddAsync(credencial, cancellationToken);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // Violación del índice único (Username, Emisor): la combinación ya existe.
            _context.Entry(credencial).State = EntityState.Detached;
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> EliminarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Credencial? credencial = await _context.Credenciales.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (credencial is null)
        {
            return false;
        }

        _context.Credenciales.Remove(credencial);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

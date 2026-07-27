using Microsoft.EntityFrameworkCore;
using SsoAdmin.Models;

namespace SsoAdmin.Data.Repositories;

/// <summary>Acceso a datos de <see cref="LoginSI"/> (cuentas de Seguridad Informática).</summary>
public interface ILoginSIRepository
{
    /// <summary>Obtiene la cuenta de SI por su nombre de usuario, o <c>null</c> si no existe.</summary>
    Task<LoginSI?> ObtenerPorUsuarioAsync(string usuario, CancellationToken cancellationToken = default);

    /// <summary>Indica si ya existe al menos una cuenta de SI (usado por el seeder).</summary>
    Task<bool> ExisteAlgunaAsync(CancellationToken cancellationToken = default);

    /// <summary>Agrega una cuenta de SI y persiste el cambio.</summary>
    Task AgregarAsync(LoginSI login, CancellationToken cancellationToken = default);
}

/// <summary>Implementación EF Core de <see cref="ILoginSIRepository"/>.</summary>
public class LoginSIRepository : ILoginSIRepository
{
    private readonly SsoAdminDbContext _context;

    /// <summary>Crea el repositorio con el contexto inyectado.</summary>
    public LoginSIRepository(SsoAdminDbContext context) => _context = context;

    /// <inheritdoc />
    public async Task<LoginSI?> ObtenerPorUsuarioAsync(string usuario, CancellationToken cancellationToken = default) =>
        await _context.LoginsSI.AsNoTracking().FirstOrDefaultAsync(l => l.Usuario == usuario, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> ExisteAlgunaAsync(CancellationToken cancellationToken = default) =>
        await _context.LoginsSI.AnyAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AgregarAsync(LoginSI login, CancellationToken cancellationToken = default)
    {
        await _context.LoginsSI.AddAsync(login, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

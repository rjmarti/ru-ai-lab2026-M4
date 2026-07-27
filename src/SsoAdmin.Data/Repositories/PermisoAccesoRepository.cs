using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SsoAdmin.Models;

namespace SsoAdmin.Data.Repositories;

/// <summary>Resultado de intentar otorgar un permiso.</summary>
public enum ResultadoOtorgarPermiso
{
    /// <summary>El permiso se otorgó correctamente.</summary>
    Otorgado,

    /// <summary>El período se solapa con uno existente; no se otorgó.</summary>
    Solapado
}

/// <summary>Acceso a datos de <see cref="PermisoAcceso"/>.</summary>
public interface IPermisoAccesoRepository
{
    /// <summary>Lista permisos, con filtros opcionales por usuario y/o aplicación.</summary>
    Task<IReadOnlyList<PermisoAcceso>> ListarAsync(Guid? usuarioId, Guid? aplicacionId, CancellationToken cancellationToken = default);

    /// <summary>Obtiene un permiso por su identificador, o <c>null</c> si no existe.</summary>
    Task<PermisoAcceso?> ObtenerAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Lista los permisos de un usuario para una aplicación específica (sin tracking).</summary>
    Task<IReadOnlyList<PermisoAcceso>> ListarPorUsuarioAplicacionAsync(Guid usuarioId, Guid aplicacionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Otorga un permiso verificando, dentro de una transacción <see cref="IsolationLevel.Serializable"/>,
    /// que su período no se solape con uno existente del mismo usuario y aplicación (FR-004).
    /// </summary>
    Task<ResultadoOtorgarPermiso> OtorgarAsync(PermisoAcceso permiso, CancellationToken cancellationToken = default);

    /// <summary>Persiste los cambios pendientes en la unidad de trabajo (p. ej. revocación).</summary>
    Task GuardarCambiosAsync(CancellationToken cancellationToken = default);

    /// <summary>Indica si dos períodos de permiso se solapan (tratando <c>FechaHasta null</c> como infinito).</summary>
    static bool SeSolapan(PermisoAcceso a, PermisoAcceso b)
    {
        bool aEmpiezaAntesDeQueBTermine = b.FechaHasta is null || a.FechaDesde <= b.FechaHasta;
        bool bEmpiezaAntesDeQueATermine = a.FechaHasta is null || b.FechaDesde <= a.FechaHasta;
        return aEmpiezaAntesDeQueBTermine && bEmpiezaAntesDeQueATermine;
    }
}

/// <summary>Implementación EF Core de <see cref="IPermisoAccesoRepository"/>.</summary>
public class PermisoAccesoRepository : IPermisoAccesoRepository
{
    private readonly SsoAdminDbContext _context;

    /// <summary>Crea el repositorio con el contexto inyectado.</summary>
    public PermisoAccesoRepository(SsoAdminDbContext context) => _context = context;

    /// <inheritdoc />
    public async Task<IReadOnlyList<PermisoAcceso>> ListarAsync(Guid? usuarioId, Guid? aplicacionId, CancellationToken cancellationToken = default)
    {
        IQueryable<PermisoAcceso> consulta = _context.Permisos.AsNoTracking();
        if (usuarioId is not null)
        {
            consulta = consulta.Where(p => p.UsuarioId == usuarioId);
        }

        if (aplicacionId is not null)
        {
            consulta = consulta.Where(p => p.AplicacionId == aplicacionId);
        }

        return await consulta.OrderBy(p => p.FechaDesde).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PermisoAcceso?> ObtenerAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Permisos.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<PermisoAcceso>> ListarPorUsuarioAplicacionAsync(Guid usuarioId, Guid aplicacionId, CancellationToken cancellationToken = default) =>
        await _context.Permisos.AsNoTracking()
            .Where(p => p.UsuarioId == usuarioId && p.AplicacionId == aplicacionId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<ResultadoOtorgarPermiso> OtorgarAsync(PermisoAcceso permiso, CancellationToken cancellationToken = default)
    {
        await using IDbContextTransaction transaccion =
            await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        List<PermisoAcceso> existentes = await _context.Permisos
            .Where(p => p.UsuarioId == permiso.UsuarioId && p.AplicacionId == permiso.AplicacionId)
            .ToListAsync(cancellationToken);

        if (existentes.Any(e => IPermisoAccesoRepository.SeSolapan(permiso, e)))
        {
            await transaccion.RollbackAsync(cancellationToken);
            return ResultadoOtorgarPermiso.Solapado;
        }

        await _context.Permisos.AddAsync(permiso, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaccion.CommitAsync(cancellationToken);
        return ResultadoOtorgarPermiso.Otorgado;
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync(CancellationToken cancellationToken = default) =>
        await _context.SaveChangesAsync(cancellationToken);
}

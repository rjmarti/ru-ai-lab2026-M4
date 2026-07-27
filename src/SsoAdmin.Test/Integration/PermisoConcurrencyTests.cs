using Microsoft.EntityFrameworkCore;
using SsoAdmin.Data;
using SsoAdmin.Data.Repositories;
using SsoAdmin.Models;

namespace SsoAdmin.Test.Integration;

/// <summary>
/// Verifica que, ante dos otorgamientos concurrentes de permisos con períodos solapados para el
/// mismo usuario y aplicación, solo uno se persista (FR-004, edge case de concurrencia). Usa
/// SQLite en archivo para tener conexiones concurrentes reales con locking del motor.
/// </summary>
public class PermisoConcurrencyTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"sso-permiso-{Guid.NewGuid():N}.db");
    private readonly Guid _usuarioId = Guid.NewGuid();
    private readonly Guid _aplicacionId = Guid.NewGuid();

    private DbContextOptions<SsoAdminDbContext> Opciones() =>
        new DbContextOptionsBuilder<SsoAdminDbContext>()
            .UseSqlite($"Data Source={_dbPath};Pooling=False")
            .Options;

    [Fact]
    public async Task Dos_otorgamientos_concurrentes_solapados_persisten_solo_uno()
    {
        await using (SsoAdminDbContext seed = new(Opciones()))
        {
            await seed.Database.EnsureCreatedAsync();
            seed.Add(new Usuario { Id = _usuarioId, Nombre = "Ana", Activo = true });
            seed.Add(new Aplicacion { Id = _aplicacionId, Nombre = "App", Url = "https://app.test" });
            await seed.SaveChangesAsync();
        }

        async Task<ResultadoOtorgarPermiso> OtorgarAsync(DateOnly desde, DateOnly hasta)
        {
            await using SsoAdminDbContext context = new(Opciones());
            PermisoAccesoRepository repo = new(context);
            return await repo.OtorgarAsync(new PermisoAcceso
            {
                Id = Guid.NewGuid(),
                UsuarioId = _usuarioId,
                AplicacionId = _aplicacionId,
                FechaDesde = desde,
                FechaHasta = hasta
            });
        }

        ResultadoOtorgarPermiso[] resultados = await Task.WhenAll(
            Task.Run(() => OtorgarAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30))),
            Task.Run(() => OtorgarAsync(new DateOnly(2026, 3, 1), new DateOnly(2026, 12, 31))));

        Assert.Equal(1, resultados.Count(r => r == ResultadoOtorgarPermiso.Otorgado));

        await using SsoAdminDbContext verificacion = new(Opciones());
        int total = await verificacion.Permisos
            .CountAsync(p => p.UsuarioId == _usuarioId && p.AplicacionId == _aplicacionId);
        Assert.Equal(1, total);
    }

    /// <summary>Elimina el archivo de base de datos temporal.</summary>
    public void Dispose()
    {
        try
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }
        catch (IOException)
        {
            // Archivo temporal; su limpieza no debe afectar el resultado del test.
        }

        GC.SuppressFinalize(this);
    }
}

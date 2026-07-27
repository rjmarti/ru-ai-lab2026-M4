using Microsoft.EntityFrameworkCore;
using SsoAdmin.Data;
using SsoAdmin.Data.Repositories;
using SsoAdmin.Models;

namespace SsoAdmin.Test.Integration;

/// <summary>
/// Verifica que, ante dos creaciones concurrentes de una credencial con la misma combinación
/// <c>(Username, Emisor)</c>, solo una se persista, gracias al índice único a nivel de base de
/// datos (FR-001, edge case de concurrencia). Usa SQLite en archivo para permitir conexiones
/// concurrentes reales con locking del motor.
/// </summary>
public class CredencialConcurrencyTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"sso-concurrency-{Guid.NewGuid():N}.db");

    private DbContextOptions<SsoAdminDbContext> Opciones() =>
        new DbContextOptionsBuilder<SsoAdminDbContext>()
            .UseSqlite($"Data Source={_dbPath};Pooling=False")
            .Options;

    [Fact]
    public async Task Dos_creaciones_concurrentes_de_la_misma_credencial_persisten_solo_una()
    {
        Guid usuarioId = Guid.NewGuid();
        await using (SsoAdminDbContext seed = new(Opciones()))
        {
            await seed.Database.EnsureCreatedAsync();
            seed.Usuarios.Add(new Usuario { Id = usuarioId, Nombre = "Ana", Activo = true });
            await seed.SaveChangesAsync();
        }

        async Task<bool> CrearAsync()
        {
            await using SsoAdminDbContext context = new(Opciones());
            CredencialRepository repo = new(context);
            return await repo.IntentarCrearAsync(new Credencial
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuarioId,
                Username = "u1",
                Emisor = "google"
            });
        }

        bool[] resultados = await Task.WhenAll(Task.Run(CrearAsync), Task.Run(CrearAsync));

        Assert.Equal(1, resultados.Count(ok => ok));

        await using SsoAdminDbContext verificacion = new(Opciones());
        int total = await verificacion.Credenciales.CountAsync(c => c.Username == "u1" && c.Emisor == "google");
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

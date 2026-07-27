using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SsoAdmin.Data;

namespace SsoAdmin.Test.TestFixtures;

/// <summary>
/// <see cref="WebApplicationFactory{TEntryPoint}"/> que sustituye el proveedor SQL Server por
/// SQLite relacional (research.md #7), usando una conexión <c>:memory:</c> compartida y
/// persistente durante la vida de la fábrica. SQLite —a diferencia del proveedor InMemory de
/// EF Core— aplica restricciones únicas reales, necesarias para verificar los criterios de
/// concurrencia (FR-001/FR-004, Constitution Principio III).
/// </summary>
/// <typeparam name="TEntryPoint">Tipo marcador del ensamblado host bajo prueba.</typeparam>
public class SqliteWebApplicationFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint>
    where TEntryPoint : class
{
    private readonly SqliteConnection _connection;

    /// <summary>Crea la fábrica y abre la conexión SQLite en memoria compartida.</summary>
    public SqliteWebApplicationFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            // Quitar la configuración de EF Core del host (SQL Server) por completo: en EF Core
            // el registro incluye DbContextOptions<T> y IDbContextOptionsConfiguration<T>; si no
            // se remueven ambos, quedan dos proveedores configurados sobre el mismo contexto.
            services.RemoveAll<DbContextOptions<SsoAdminDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<IDbContextOptionsConfiguration<SsoAdminDbContext>>();
            services.RemoveAll<SsoAdminDbContext>();

            services.AddDbContext<SsoAdminDbContext>(options => options.UseSqlite(_connection));
        });
    }

    /// <summary>Ejecuta una acción con un <see cref="SsoAdminDbContext"/> en un scope nuevo.</summary>
    public async Task EnUnContextoAsync(Func<SsoAdminDbContext, Task> accion)
    {
        using IServiceScope scope = Services.CreateScope();
        SsoAdminDbContext context = scope.ServiceProvider.GetRequiredService<SsoAdminDbContext>();
        await accion(context);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}

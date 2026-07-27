using Microsoft.EntityFrameworkCore;
using SsoAdmin.Test.TestFixtures;

namespace SsoAdmin.Test.Integration;

/// <summary>
/// Valida el checkpoint de la fase Foundational: ambos hosts arrancan sobre SQLite, el
/// esquema se crea y la app Web precarga la cuenta <c>admin</c> (FR-007).
/// </summary>
public class FoundationSmokeTests
{
    [Fact]
    public async Task Web_host_arranca_y_precarga_la_cuenta_admin()
    {
        using WebTestFactory factory = new();
        // Forzar el arranque del host (ejecuta la inicialización de BD y el seeder).
        _ = factory.CreateClient();

        await factory.EnUnContextoAsync(async context =>
        {
            bool existeAdmin = await context.LoginsSI.AnyAsync(l => l.Usuario == "admin");
            Assert.True(existeAdmin, "El seeder debe precargar la cuenta 'admin' en el primer arranque.");
        });
    }

    [Fact]
    public async Task Api_host_arranca_y_crea_el_esquema()
    {
        using ApiTestFactory factory = new();
        _ = factory.CreateClient();

        await factory.EnUnContextoAsync(async context =>
        {
            // Si el esquema existe, esta consulta no lanza excepción.
            int usuarios = await context.Usuarios.CountAsync();
            Assert.Equal(0, usuarios);
        });
    }
}

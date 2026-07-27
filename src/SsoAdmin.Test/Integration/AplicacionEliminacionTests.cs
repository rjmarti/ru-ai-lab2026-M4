using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SsoAdmin.Models;
using SsoAdmin.Test.TestFixtures;

namespace SsoAdmin.Test.Integration;

/// <summary>
/// Verifica el edge case de US4: al eliminar una aplicación con permisos activos, una consulta
/// SSO posterior para esa URL responde <c>motivo=aplicacion_no_encontrada</c>.
/// </summary>
public class AplicacionEliminacionTests
{
    private sealed record VerificarResp(bool Allowed, string? Motivo);

    [Fact]
    public async Task Eliminar_aplicacion_con_permisos_hace_que_el_sso_responda_aplicacion_no_encontrada()
    {
        using ApiTestFactory factory = new();
        DateOnly hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        Guid usuarioId = Guid.NewGuid();
        Guid aplicacionId = Guid.NewGuid();

        await factory.EnUnContextoAsync(async context =>
        {
            context.Add(new Usuario { Id = usuarioId, Nombre = "Ana", Activo = true });
            context.Add(new Aplicacion { Id = aplicacionId, Nombre = "App", Url = "https://app.test" });
            context.Add(new Credencial { Id = Guid.NewGuid(), UsuarioId = usuarioId, Username = "u1", Emisor = "google" });
            context.Add(new PermisoAcceso { Id = Guid.NewGuid(), UsuarioId = usuarioId, AplicacionId = aplicacionId, FechaDesde = hoy.AddDays(-5), FechaHasta = null });
            await context.SaveChangesAsync();
        });

        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ApiTestFactory.ApiKey);
        object request = new { username = "u1", emisor = "google", aplicacionUrl = "https://app.test" };

        VerificarResp antes = (await (await client.PostAsJsonAsync("/api/sso/verificar", request))
            .Content.ReadFromJsonAsync<VerificarResp>())!;
        Assert.True(antes.Allowed);

        // Eliminar la aplicación (sus permisos se eliminan en cascada).
        await factory.EnUnContextoAsync(async context =>
        {
            Aplicacion app = await context.Aplicaciones.SingleAsync(a => a.Id == aplicacionId);
            context.Aplicaciones.Remove(app);
            await context.SaveChangesAsync();
        });

        VerificarResp despues = (await (await client.PostAsJsonAsync("/api/sso/verificar", request))
            .Content.ReadFromJsonAsync<VerificarResp>())!;

        Assert.False(despues.Allowed);
        Assert.Equal("aplicacion_no_encontrada", despues.Motivo);
    }
}

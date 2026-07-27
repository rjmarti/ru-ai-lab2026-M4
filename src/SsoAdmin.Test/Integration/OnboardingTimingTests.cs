using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SsoAdmin.Test.TestFixtures;

namespace SsoAdmin.Test.Integration;

/// <summary>
/// Aproxima SC-007: el ciclo de alta de un usuario, su credencial y su permiso a una aplicación
/// se completa en menos de 2 minutos. Se ejerce el flujo a través de la API interna de
/// administración (mismo backend que consume la UI), como cota superior automatizada del tiempo.
/// </summary>
public class OnboardingTimingTests
{
    [Fact]
    public async Task Ciclo_de_alta_usuario_credencial_permiso_se_completa_en_menos_de_2min()
    {
        using WebTestFactory factory = new();
        HttpClient client = await factory.CrearClienteAdminAsync();
        DateOnly hoy = DateOnly.FromDateTime(DateTime.UtcNow);

        Stopwatch cronometro = Stopwatch.StartNew();

        JsonElement usuario = await (await client.PostAsJsonAsync("/api/usuarios", new { nombre = "Ana" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Guid usuarioId = usuario.GetProperty("id").GetGuid();

        JsonElement app = await (await client.PostAsJsonAsync("/api/aplicaciones", new { nombre = "App", url = "https://app.test" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Guid aplicacionId = app.GetProperty("id").GetGuid();

        HttpResponseMessage credencial = await client.PostAsJsonAsync("/api/credenciales",
            new { usuarioId, username = "u1", emisor = "google" });
        Assert.Equal(HttpStatusCode.OK, credencial.StatusCode);

        HttpResponseMessage permiso = await client.PostAsJsonAsync("/api/permisos",
            new { usuarioId, aplicacionId, fechaDesde = hoy.ToString("yyyy-MM-dd"), fechaHasta = (string?)null });
        Assert.Equal(HttpStatusCode.OK, permiso.StatusCode);

        cronometro.Stop();

        Assert.True(cronometro.Elapsed < TimeSpan.FromMinutes(2),
            $"El ciclo de alta tardó {cronometro.Elapsed} (objetivo <2 min).");
    }
}

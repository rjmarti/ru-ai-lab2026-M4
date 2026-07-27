using System.Diagnostics;
using System.Net.Http.Json;
using SsoAdmin.Models;
using SsoAdmin.Test.TestFixtures;

namespace SsoAdmin.Test.Integration;

/// <summary>
/// Verifica el objetivo de rendimiento SC-001/FR-014: <c>POST /api/sso/verificar</c> responde
/// en menos de 500 ms con una carga de referencia de 100 aplicaciones y 3000 usuarios.
/// </summary>
public class SsoVerificarPerformanceTests
{
    private sealed record VerificarResp(bool Allowed, string? Motivo);

    [Fact]
    public async Task Verificar_responde_en_menos_de_500ms_con_carga_de_referencia()
    {
        using ApiTestFactory factory = new();
        DateOnly hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        Guid usuarioObjetivo = Guid.NewGuid();
        Guid appObjetivo = Guid.NewGuid();

        await factory.EnUnContextoAsync(async context =>
        {
            List<Usuario> usuarios = new(3000);
            for (int i = 0; i < 3000; i++)
            {
                usuarios.Add(new Usuario { Id = Guid.NewGuid(), Nombre = $"Usuario {i}", Activo = true });
            }
            usuarios[0].Id = usuarioObjetivo;

            List<Aplicacion> apps = new(100);
            for (int i = 0; i < 100; i++)
            {
                apps.Add(new Aplicacion { Id = Guid.NewGuid(), Nombre = $"App {i}", Url = $"https://app{i}.test" });
            }
            apps[0].Id = appObjetivo;
            apps[0].Url = "https://objetivo.test";

            context.Usuarios.AddRange(usuarios);
            context.Aplicaciones.AddRange(apps);
            context.Credenciales.Add(new Credencial { Id = Guid.NewGuid(), UsuarioId = usuarioObjetivo, Username = "u1", Emisor = "google" });
            context.Permisos.Add(new PermisoAcceso { Id = Guid.NewGuid(), UsuarioId = usuarioObjetivo, AplicacionId = appObjetivo, FechaDesde = hoy.AddDays(-1), FechaHasta = null });
            await context.SaveChangesAsync();
        });

        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ApiTestFactory.ApiKey);
        object request = new { username = "u1", emisor = "google", aplicacionUrl = "https://objetivo.test" };

        // Warm-up para excluir el costo de JIT/primer request.
        await client.PostAsJsonAsync("/api/sso/verificar", request);

        Stopwatch cronometro = Stopwatch.StartNew();
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/sso/verificar", request);
        cronometro.Stop();

        VerificarResp body = (await response.Content.ReadFromJsonAsync<VerificarResp>())!;
        Assert.True(body.Allowed);
        Assert.True(cronometro.ElapsedMilliseconds < 500,
            $"La verificación tardó {cronometro.ElapsedMilliseconds} ms (objetivo <500 ms).");
    }
}

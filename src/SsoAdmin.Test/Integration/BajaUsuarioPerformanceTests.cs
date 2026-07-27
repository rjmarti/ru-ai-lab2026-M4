using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SsoAdmin.Models;
using SsoAdmin.Test.TestFixtures;

namespace SsoAdmin.Test.Integration;

/// <summary>
/// Verifica el objetivo de rendimiento SC-002/FR-015: la baja lógica de un usuario con permisos
/// en múltiples aplicaciones caduca todos sus permisos en menos de 3 segundos.
/// </summary>
public class BajaUsuarioPerformanceTests
{
    [Fact]
    public async Task Baja_de_usuario_con_muchos_permisos_tarda_menos_de_3s()
    {
        using WebTestFactory factory = new();
        HttpClient client = await factory.CrearClienteAdminAsync();
        DateOnly hoy = DateOnly.FromDateTime(DateTime.UtcNow);

        JsonElement usuario = await (await client.PostAsJsonAsync("/api/usuarios", new { nombre = "Ana" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Guid usuarioId = usuario.GetProperty("id").GetGuid();

        await factory.EnUnContextoAsync(async context =>
        {
            List<Aplicacion> apps = new(100);
            List<PermisoAcceso> permisos = new(100);
            for (int i = 0; i < 100; i++)
            {
                Guid appId = Guid.NewGuid();
                apps.Add(new Aplicacion { Id = appId, Nombre = $"App {i}", Url = $"https://app{i}.test" });
                permisos.Add(new PermisoAcceso { Id = Guid.NewGuid(), UsuarioId = usuarioId, AplicacionId = appId, FechaDesde = hoy.AddDays(-1), FechaHasta = null });
            }

            context.Aplicaciones.AddRange(apps);
            context.Permisos.AddRange(permisos);
            await context.SaveChangesAsync();
        });

        Stopwatch cronometro = Stopwatch.StartNew();
        HttpResponseMessage baja = await client.PostAsync($"/api/usuarios/{usuarioId}/baja", null);
        cronometro.Stop();

        Assert.Equal(HttpStatusCode.OK, baja.StatusCode);
        Assert.True(cronometro.ElapsedMilliseconds < 3000,
            $"La baja tardó {cronometro.ElapsedMilliseconds} ms (objetivo <3000 ms).");

        await factory.EnUnContextoAsync(async context =>
        {
            bool todosCaducados = context.Permisos
                .Where(p => p.UsuarioId == usuarioId)
                .All(p => p.FechaHasta != null && p.FechaHasta <= hoy);
            Assert.True(todosCaducados);
            await Task.CompletedTask;
        });
    }
}

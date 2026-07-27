using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SsoAdmin.Test.TestFixtures;

namespace SsoAdmin.Test.Integration;

/// <summary>
/// Tests de integración de <c>/api/permisos</c> (US4, AC2/AC3): solapamiento de períodos → 409
/// (incluye coincidencia exacta y permiso indefinido), <c>fecha_desde &gt; fecha_hasta</c> → 400,
/// y revocación que fija <c>fecha_hasta = hoy</c> (FR-005).
/// </summary>
public class PermisosControllerTests
{
    private static async Task<(Guid UsuarioId, Guid AplicacionId)> CrearUsuarioYAplicacionAsync(HttpClient client)
    {
        JsonElement usuario = await (await client.PostAsJsonAsync("/api/usuarios", new { nombre = "Ana" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        JsonElement app = await (await client.PostAsJsonAsync("/api/aplicaciones", new { nombre = "App", url = "https://app.test" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        return (usuario.GetProperty("id").GetGuid(), app.GetProperty("id").GetGuid());
    }

    [Fact] // AC2
    public async Task Periodo_solapado_devuelve_409()
    {
        using WebTestFactory factory = new();
        HttpClient client = await factory.CrearClienteAdminAsync();
        (Guid usuarioId, Guid aplicacionId) = await CrearUsuarioYAplicacionAsync(client);

        await client.PostAsJsonAsync("/api/permisos", new { usuarioId, aplicacionId, fechaDesde = "2026-01-01", fechaHasta = "2026-06-30" });

        HttpResponseMessage solapado = await client.PostAsJsonAsync("/api/permisos",
            new { usuarioId, aplicacionId, fechaDesde = "2026-03-01", fechaHasta = "2026-12-31" });

        Assert.Equal(HttpStatusCode.Conflict, solapado.StatusCode);
    }

    [Fact] // AC2 edge — coincidencia exacta
    public async Task Periodo_identico_devuelve_409()
    {
        using WebTestFactory factory = new();
        HttpClient client = await factory.CrearClienteAdminAsync();
        (Guid usuarioId, Guid aplicacionId) = await CrearUsuarioYAplicacionAsync(client);

        await client.PostAsJsonAsync("/api/permisos", new { usuarioId, aplicacionId, fechaDesde = "2026-01-01", fechaHasta = "2026-06-30" });

        HttpResponseMessage identico = await client.PostAsJsonAsync("/api/permisos",
            new { usuarioId, aplicacionId, fechaDesde = "2026-01-01", fechaHasta = "2026-06-30" });

        Assert.Equal(HttpStatusCode.Conflict, identico.StatusCode);
    }

    [Fact] // AC2 edge — permiso indefinido previo
    public async Task Periodo_posterior_a_uno_indefinido_devuelve_409()
    {
        using WebTestFactory factory = new();
        HttpClient client = await factory.CrearClienteAdminAsync();
        (Guid usuarioId, Guid aplicacionId) = await CrearUsuarioYAplicacionAsync(client);

        await client.PostAsJsonAsync("/api/permisos", new { usuarioId, aplicacionId, fechaDesde = "2026-01-01", fechaHasta = (string?)null });

        HttpResponseMessage posterior = await client.PostAsJsonAsync("/api/permisos",
            new { usuarioId, aplicacionId, fechaDesde = "2026-06-01", fechaHasta = "2026-12-31" });

        Assert.Equal(HttpStatusCode.Conflict, posterior.StatusCode);
    }

    [Fact] // edge — fecha_desde > fecha_hasta
    public async Task Fecha_desde_posterior_a_fecha_hasta_devuelve_400()
    {
        using WebTestFactory factory = new();
        HttpClient client = await factory.CrearClienteAdminAsync();
        (Guid usuarioId, Guid aplicacionId) = await CrearUsuarioYAplicacionAsync(client);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/permisos",
            new { usuarioId, aplicacionId, fechaDesde = "2026-12-31", fechaHasta = "2026-01-01" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact] // AC3
    public async Task Revocar_fija_fecha_hasta_en_hoy()
    {
        using WebTestFactory factory = new();
        HttpClient client = await factory.CrearClienteAdminAsync();
        (Guid usuarioId, Guid aplicacionId) = await CrearUsuarioYAplicacionAsync(client);
        DateOnly hoy = DateOnly.FromDateTime(DateTime.UtcNow);

        JsonElement permiso = await (await client.PostAsJsonAsync("/api/permisos",
            new { usuarioId, aplicacionId, fechaDesde = hoy.AddDays(-10).ToString("yyyy-MM-dd"), fechaHasta = (string?)null }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Guid permisoId = permiso.GetProperty("id").GetGuid();

        HttpResponseMessage revocado = await client.PostAsync($"/api/permisos/{permisoId}/revocar", null);
        Assert.Equal(HttpStatusCode.OK, revocado.StatusCode);

        await factory.EnUnContextoAsync(async context =>
        {
            var recargado = await context.Permisos.SingleAsync(p => p.Id == permisoId);
            Assert.Equal(hoy, recargado.FechaHasta);
        });
    }
}

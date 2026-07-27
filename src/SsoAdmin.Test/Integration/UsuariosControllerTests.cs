using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SsoAdmin.Models;
using SsoAdmin.Test.TestFixtures;

namespace SsoAdmin.Test.Integration;

/// <summary>
/// Tests de integración de <c>/api/usuarios</c> (US2, AC3/AC4): listar/crear/editar/baja,
/// incluyendo la cascada de permisos al dar de baja (FR-006) y la idempotencia.
/// </summary>
public class UsuariosControllerTests
{
    private sealed record UsuarioDto(Guid Id, string Nombre, bool Activo);

    private static async Task<HttpClient> ClienteAutenticadoAsync(WebTestFactory factory)
    {
        HttpClient client = factory.CreateClient();
        HttpResponseMessage login = await client.PostAsJsonAsync("/api/auth/login",
            new { usuario = "admin", password = "admin" });
        login.EnsureSuccessStatusCode();
        return client;
    }

    [Fact]
    public async Task Crear_listar_y_editar_usuario_refleja_el_estado()
    {
        using WebTestFactory factory = new();
        HttpClient client = await ClienteAutenticadoAsync(factory);

        HttpResponseMessage creado = await client.PostAsJsonAsync("/api/usuarios", new { nombre = "Ana" });
        Assert.Equal(HttpStatusCode.OK, creado.StatusCode);
        UsuarioDto usuario = (await creado.Content.ReadFromJsonAsync<UsuarioDto>())!;

        UsuarioDto[] lista = (await client.GetFromJsonAsync<UsuarioDto[]>("/api/usuarios"))!;
        Assert.Contains(lista, u => u.Id == usuario.Id && u.Nombre == "Ana" && u.Activo);

        HttpResponseMessage editado = await client.PutAsJsonAsync($"/api/usuarios/{usuario.Id}", new { nombre = "Ana María" });
        Assert.Equal(HttpStatusCode.OK, editado.StatusCode);

        UsuarioDto[] listaEditada = (await client.GetFromJsonAsync<UsuarioDto[]>("/api/usuarios"))!;
        Assert.Contains(listaEditada, u => u.Id == usuario.Id && u.Nombre == "Ana María");
    }

    [Fact]
    public async Task Crear_usuario_con_nombre_vacio_devuelve_400()
    {
        using WebTestFactory factory = new();
        HttpClient client = await ClienteAutenticadoAsync(factory);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/usuarios", new { nombre = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Baja_caduca_permisos_activos_y_es_idempotente()
    {
        using WebTestFactory factory = new();
        HttpClient client = await ClienteAutenticadoAsync(factory);
        DateOnly hoy = DateOnly.FromDateTime(DateTime.UtcNow);

        UsuarioDto usuario = (await (await client.PostAsJsonAsync("/api/usuarios", new { nombre = "Ana" }))
            .Content.ReadFromJsonAsync<UsuarioDto>())!;

        // Sembrar una aplicación y dos permisos activos para el usuario recién creado.
        Guid app1 = Guid.NewGuid();
        Guid app2 = Guid.NewGuid();
        await factory.EnUnContextoAsync(async context =>
        {
            context.AddRange(
                new Aplicacion { Id = app1, Nombre = "App1", Url = "https://app1.test" },
                new Aplicacion { Id = app2, Nombre = "App2", Url = "https://app2.test" },
                new PermisoAcceso { Id = Guid.NewGuid(), UsuarioId = usuario.Id, AplicacionId = app1, FechaDesde = hoy.AddDays(-5), FechaHasta = null },
                new PermisoAcceso { Id = Guid.NewGuid(), UsuarioId = usuario.Id, AplicacionId = app2, FechaDesde = hoy.AddDays(-5), FechaHasta = hoy.AddDays(30) });
            await context.SaveChangesAsync();
        });

        HttpResponseMessage baja = await client.PostAsync($"/api/usuarios/{usuario.Id}/baja", null);
        Assert.Equal(HttpStatusCode.OK, baja.StatusCode);

        await factory.EnUnContextoAsync(async context =>
        {
            Usuario recargado = await context.Usuarios.SingleAsync(u => u.Id == usuario.Id);
            Assert.False(recargado.Activo);

            List<PermisoAcceso> permisos = await context.Permisos
                .Where(p => p.UsuarioId == usuario.Id).ToListAsync();
            Assert.All(permisos, p => Assert.True(p.FechaHasta is not null && p.FechaHasta <= hoy));
        });

        // Idempotencia: dar de baja nuevamente no produce error.
        HttpResponseMessage bajaRepetida = await client.PostAsync($"/api/usuarios/{usuario.Id}/baja", null);
        Assert.Equal(HttpStatusCode.OK, bajaRepetida.StatusCode);
    }
}

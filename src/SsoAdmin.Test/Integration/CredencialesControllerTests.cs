using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SsoAdmin.Data;
using SsoAdmin.Models;
using SsoAdmin.Test.TestFixtures;

namespace SsoAdmin.Test.Integration;

/// <summary>
/// Tests de integración de <c>/api/credenciales</c> (US3): unicidad (AC1/AC2),
/// listar/crear/eliminar (AC3), ausencia de campos de contraseña (AC4/SC-006) y el caso
/// positivo de FR-002 (mismo username, distinto emisor).
/// </summary>
public class CredencialesControllerTests
{
    private sealed record CredencialDto(Guid Id, Guid UsuarioId, string UsuarioNombre, string Username, string Emisor);

    private static async Task<Guid> CrearUsuarioAsync(HttpClient client, string nombre)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/usuarios", new { nombre });
        response.EnsureSuccessStatusCode();
        System.Text.Json.JsonElement dto = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        return dto.GetProperty("id").GetGuid();
    }

    [Fact] // AC1
    public async Task Credencial_duplicada_devuelve_400()
    {
        using WebTestFactory factory = new();
        HttpClient client = await factory.CrearClienteAdminAsync();
        Guid usuarioId = await CrearUsuarioAsync(client, "Ana");

        HttpResponseMessage primera = await client.PostAsJsonAsync("/api/credenciales",
            new { usuarioId, username = "u1", emisor = "google" });
        Assert.Equal(HttpStatusCode.OK, primera.StatusCode);

        HttpResponseMessage duplicada = await client.PostAsJsonAsync("/api/credenciales",
            new { usuarioId, username = "u1", emisor = "google" });
        Assert.Equal(HttpStatusCode.BadRequest, duplicada.StatusCode);
    }

    [Fact] // AC2
    public async Task Reasignar_credencial_existente_a_otro_usuario_devuelve_400()
    {
        using WebTestFactory factory = new();
        HttpClient client = await factory.CrearClienteAdminAsync();
        Guid usuarioA = await CrearUsuarioAsync(client, "Ana");
        Guid usuarioB = await CrearUsuarioAsync(client, "Beto");

        await client.PostAsJsonAsync("/api/credenciales", new { usuarioId = usuarioA, username = "u1", emisor = "google" });

        HttpResponseMessage reasignada = await client.PostAsJsonAsync("/api/credenciales",
            new { usuarioId = usuarioB, username = "u1", emisor = "google" });
        Assert.Equal(HttpStatusCode.BadRequest, reasignada.StatusCode);
    }

    [Fact] // AC3
    public async Task Listar_crear_y_eliminar_credencial()
    {
        using WebTestFactory factory = new();
        HttpClient client = await factory.CrearClienteAdminAsync();
        Guid usuarioId = await CrearUsuarioAsync(client, "Ana");

        CredencialDto creada = (await (await client.PostAsJsonAsync("/api/credenciales",
            new { usuarioId, username = "u1", emisor = "google" })).Content.ReadFromJsonAsync<CredencialDto>())!;

        CredencialDto[] lista = (await client.GetFromJsonAsync<CredencialDto[]>("/api/credenciales"))!;
        Assert.Contains(lista, c => c.Id == creada.Id && c.UsuarioNombre == "Ana");

        HttpResponseMessage eliminada = await client.DeleteAsync($"/api/credenciales/{creada.Id}");
        Assert.Equal(HttpStatusCode.OK, eliminada.StatusCode);

        CredencialDto[] listaFinal = (await client.GetFromJsonAsync<CredencialDto[]>("/api/credenciales"))!;
        Assert.DoesNotContain(listaFinal, c => c.Id == creada.Id);
    }

    [Fact] // FR-002 positivo
    public async Task Mismo_username_distinto_emisor_se_permite()
    {
        using WebTestFactory factory = new();
        HttpClient client = await factory.CrearClienteAdminAsync();
        Guid usuarioId = await CrearUsuarioAsync(client, "Ana");

        HttpResponseMessage google = await client.PostAsJsonAsync("/api/credenciales", new { usuarioId, username = "u1", emisor = "google" });
        HttpResponseMessage microsoft = await client.PostAsJsonAsync("/api/credenciales", new { usuarioId, username = "u1", emisor = "microsoft" });

        Assert.Equal(HttpStatusCode.OK, google.StatusCode);
        Assert.Equal(HttpStatusCode.OK, microsoft.StatusCode);
    }

    [Fact] // AC4 / SC-006
    public void La_entidad_credencial_no_tiene_ningun_campo_de_contrasena()
    {
        using WebTestFactory factory = new();
        using IServiceScope scope = factory.Services.CreateScope();
        SsoAdminDbContext context = scope.ServiceProvider.GetRequiredService<SsoAdminDbContext>();

        IEnumerable<string> propiedades = context.Model.FindEntityType(typeof(Credencial))!
            .GetProperties().Select(p => p.Name.ToLowerInvariant());

        Assert.DoesNotContain(propiedades, nombre =>
            nombre.Contains("password") || nombre.Contains("contrasena") || nombre.Contains("hash"));
    }
}

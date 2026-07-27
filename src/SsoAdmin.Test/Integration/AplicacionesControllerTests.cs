using System.Net;
using System.Net.Http.Json;
using SsoAdmin.Test.TestFixtures;

namespace SsoAdmin.Test.Integration;

/// <summary>
/// Tests de integración de <c>/api/aplicaciones</c> (US4, AC1/AC4): registro/edición con URL
/// vacía → 400, y listar/crear/editar/eliminar.
/// </summary>
public class AplicacionesControllerTests
{
    private sealed record AplicacionDto(Guid Id, string Nombre, string Url);

    [Fact] // AC1
    public async Task Registrar_con_url_vacia_devuelve_400()
    {
        using WebTestFactory factory = new();
        HttpClient client = await factory.CrearClienteAdminAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/aplicaciones", new { nombre = "App", url = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact] // AC4
    public async Task Listar_crear_editar_y_eliminar_aplicacion()
    {
        using WebTestFactory factory = new();
        HttpClient client = await factory.CrearClienteAdminAsync();

        AplicacionDto creada = (await (await client.PostAsJsonAsync("/api/aplicaciones",
            new { nombre = "App", url = "https://app.test" })).Content.ReadFromJsonAsync<AplicacionDto>())!;

        AplicacionDto[] lista = (await client.GetFromJsonAsync<AplicacionDto[]>("/api/aplicaciones"))!;
        Assert.Contains(lista, a => a.Id == creada.Id);

        HttpResponseMessage editada = await client.PutAsJsonAsync($"/api/aplicaciones/{creada.Id}",
            new { nombre = "App v2", url = "https://app-v2.test" });
        Assert.Equal(HttpStatusCode.OK, editada.StatusCode);

        HttpResponseMessage editUrlVacia = await client.PutAsJsonAsync($"/api/aplicaciones/{creada.Id}",
            new { nombre = "App v2", url = "" });
        Assert.Equal(HttpStatusCode.BadRequest, editUrlVacia.StatusCode);

        HttpResponseMessage eliminada = await client.DeleteAsync($"/api/aplicaciones/{creada.Id}");
        Assert.Equal(HttpStatusCode.OK, eliminada.StatusCode);

        AplicacionDto[] listaFinal = (await client.GetFromJsonAsync<AplicacionDto[]>("/api/aplicaciones"))!;
        Assert.DoesNotContain(listaFinal, a => a.Id == creada.Id);
    }
}

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SsoAdmin.API.Auth;
using SsoAdmin.Data.Repositories;
using SsoAdmin.Models;
using SsoAdmin.Test.Fakes;
using SsoAdmin.Test.TestFixtures;

namespace SsoAdmin.Test.Integration;

/// <summary>
/// Tests de integración de <c>POST /api/sso/verificar</c> cubriendo los escenarios de
/// aceptación AC1–AC8 de US1 más el caso <c>500</c> (FR-009), contra un host real con SQLite.
/// </summary>
public class SsoVerificarEndpointTests
{
    private static readonly DateOnly Hoy = DateOnly.FromDateTime(DateTime.UtcNow);

    private sealed record VerificarResp(bool Allowed, string? Motivo);

    private static HttpClient ClienteConClave(ApiTestFactory factory)
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthenticationHeader, ApiTestFactory.ApiKey);
        return client;
    }

    private const string ApiKeyAuthenticationHeader = "X-Api-Key";

    private static async Task SembrarEscenarioAsync(ApiTestFactory factory, bool usuarioActivo, PermisoBuilder? permiso)
    {
        await factory.EnUnContextoAsync(async context =>
        {
            Usuario usuario = new() { Id = Guid.NewGuid(), Nombre = "Ana", Activo = usuarioActivo };
            Aplicacion app = new() { Id = Guid.NewGuid(), Nombre = "App", Url = "https://app.test" };
            Credencial credencial = new() { Id = Guid.NewGuid(), UsuarioId = usuario.Id, Username = "u1", Emisor = "google" };
            context.AddRange(usuario, app, credencial);
            if (permiso is not null)
            {
                context.Add(new PermisoAcceso
                {
                    Id = Guid.NewGuid(),
                    UsuarioId = usuario.Id,
                    AplicacionId = app.Id,
                    FechaDesde = permiso.Desde,
                    FechaHasta = permiso.Hasta
                });
            }

            await context.SaveChangesAsync();
        });
    }

    private static async Task<(HttpStatusCode Status, VerificarResp? Body)> VerificarAsync(HttpClient client, object body)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/sso/verificar", body);
        VerificarResp? parsed = response.StatusCode == HttpStatusCode.OK
            ? await response.Content.ReadFromJsonAsync<VerificarResp>()
            : null;
        return (response.StatusCode, parsed);
    }

    private static object RequestValido() => new { username = "u1", emisor = "google", aplicacionUrl = "https://app.test" };

    private sealed record PermisoBuilder(DateOnly Desde, DateOnly? Hasta);

    [Fact] // AC1
    public async Task Credencial_valida_con_permiso_vigente_devuelve_allowed_true()
    {
        using ApiTestFactory factory = new();
        await SembrarEscenarioAsync(factory, usuarioActivo: true, new PermisoBuilder(Hoy.AddDays(-1), null));
        HttpClient client = ClienteConClave(factory);

        (HttpStatusCode status, VerificarResp? body) = await VerificarAsync(client, RequestValido());

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.True(body!.Allowed);
        Assert.Null(body.Motivo);
    }

    [Fact] // AC2
    public async Task Permiso_vencido_devuelve_permiso_vencido()
    {
        using ApiTestFactory factory = new();
        await SembrarEscenarioAsync(factory, usuarioActivo: true, new PermisoBuilder(Hoy.AddDays(-30), Hoy.AddDays(-1)));
        HttpClient client = ClienteConClave(factory);

        (HttpStatusCode status, VerificarResp? body) = await VerificarAsync(client, RequestValido());

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.False(body!.Allowed);
        Assert.Equal("permiso_vencido", body.Motivo);
    }

    [Fact] // AC3
    public async Task Usuario_inactivo_devuelve_usuario_inactivo()
    {
        using ApiTestFactory factory = new();
        await SembrarEscenarioAsync(factory, usuarioActivo: false, new PermisoBuilder(Hoy.AddDays(-1), null));
        HttpClient client = ClienteConClave(factory);

        (HttpStatusCode status, VerificarResp? body) = await VerificarAsync(client, RequestValido());

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.False(body!.Allowed);
        Assert.Equal("usuario_inactivo", body.Motivo);
    }

    [Fact] // AC4
    public async Task Aplicacion_inexistente_devuelve_aplicacion_no_encontrada()
    {
        using ApiTestFactory factory = new();
        await SembrarEscenarioAsync(factory, usuarioActivo: true, new PermisoBuilder(Hoy.AddDays(-1), null));
        HttpClient client = ClienteConClave(factory);

        (HttpStatusCode status, VerificarResp? body) = await VerificarAsync(client,
            new { username = "u1", emisor = "google", aplicacionUrl = "https://desconocida.test" });

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.False(body!.Allowed);
        Assert.Equal("aplicacion_no_encontrada", body.Motivo);
    }

    [Fact] // AC5
    public async Task Credencial_inexistente_devuelve_credencial_no_encontrada()
    {
        using ApiTestFactory factory = new();
        await SembrarEscenarioAsync(factory, usuarioActivo: true, new PermisoBuilder(Hoy.AddDays(-1), null));
        HttpClient client = ClienteConClave(factory);

        (HttpStatusCode status, VerificarResp? body) = await VerificarAsync(client,
            new { username = "inexistente", emisor = "google", aplicacionUrl = "https://app.test" });

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.False(body!.Allowed);
        Assert.Equal("credencial_no_encontrada", body.Motivo);
    }

    [Fact] // AC6
    public async Task Sin_permiso_devuelve_permiso_no_encontrado()
    {
        using ApiTestFactory factory = new();
        await SembrarEscenarioAsync(factory, usuarioActivo: true, permiso: null);
        HttpClient client = ClienteConClave(factory);

        (HttpStatusCode status, VerificarResp? body) = await VerificarAsync(client, RequestValido());

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.False(body!.Allowed);
        Assert.Equal("permiso_no_encontrado", body.Motivo);
    }

    [Fact] // AC6 edge — fecha_desde futura
    public async Task Permiso_con_fecha_desde_futura_devuelve_permiso_no_encontrado()
    {
        using ApiTestFactory factory = new();
        await SembrarEscenarioAsync(factory, usuarioActivo: true, new PermisoBuilder(Hoy.AddDays(5), null));
        HttpClient client = ClienteConClave(factory);

        (HttpStatusCode status, VerificarResp? body) = await VerificarAsync(client, RequestValido());

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.False(body!.Allowed);
        Assert.Equal("permiso_no_encontrado", body.Motivo);
    }

    [Fact] // AC7
    public async Task Campo_faltante_devuelve_400()
    {
        using ApiTestFactory factory = new();
        await SembrarEscenarioAsync(factory, usuarioActivo: true, null);
        HttpClient client = ClienteConClave(factory);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/sso/verificar",
            new { emisor = "google", aplicacionUrl = "https://app.test" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact] // AC8 — sin clave
    public async Task Sin_api_key_devuelve_401()
    {
        using ApiTestFactory factory = new();
        await SembrarEscenarioAsync(factory, usuarioActivo: true, null);
        HttpClient client = factory.CreateClient(); // sin header X-Api-Key

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/sso/verificar", RequestValido());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact] // AC8 — clave inválida
    public async Task Api_key_invalida_devuelve_401()
    {
        using ApiTestFactory factory = new();
        await SembrarEscenarioAsync(factory, usuarioActivo: true, null);
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "clave-incorrecta");

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/sso/verificar", RequestValido());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact] // FR-009 — error inesperado
    public async Task Error_inesperado_en_repositorio_devuelve_500()
    {
        using ApiTestFactory baseFactory = new();
        using var factory = baseFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
                services.Replace(ServiceDescriptor.Scoped<ICredencialRepository, FaultingCredencialRepository>())));

        HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add("X-Api-Key", ApiTestFactory.ApiKey);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/sso/verificar", RequestValido());

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}

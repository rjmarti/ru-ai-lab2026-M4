using System.Net;
using System.Net.Http.Json;
using SsoAdmin.Test.TestFixtures;

namespace SsoAdmin.Test.Integration;

/// <summary>
/// Tests de integración de <c>/api/auth</c> (US2, AC1/AC2): login válido emite cookie y
/// login inválido devuelve 401, contra el host Web con la cuenta <c>admin</c> precargada.
/// </summary>
public class AuthControllerTests
{
    [Fact]
    public async Task Login_valido_devuelve_200_y_cookie()
    {
        using WebTestFactory factory = new();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/login",
            new { usuario = "admin", password = "admin" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(response.Headers, h => h.Key == "Set-Cookie");
    }

    [Fact]
    public async Task Login_invalido_devuelve_401()
    {
        using WebTestFactory factory = new();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/login",
            new { usuario = "admin", password = "incorrecta" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Endpoint_protegido_sin_login_devuelve_401()
    {
        using WebTestFactory factory = new();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/usuarios");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

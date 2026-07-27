using System.Net;
using System.Text.Json;
using SsoAdmin.Test.TestFixtures;

namespace SsoAdmin.Test.Integration;

/// <summary>
/// Verifica que el documento OpenAPI de <c>SsoAdmin.API</c> publica el contrato del SSO
/// (<c>POST /api/sso/verificar</c>), consistente con <c>contracts/sso-verificar.md</c> (T071).
/// </summary>
public class OpenApiContractTests
{
    [Fact]
    public async Task El_documento_openapi_publica_el_endpoint_sso_verificar()
    {
        using ApiTestFactory factory = new();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using JsonDocument documento = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement paths = documento.RootElement.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/sso/verificar", out JsonElement endpoint));
        Assert.True(endpoint.TryGetProperty("post", out _));
    }
}

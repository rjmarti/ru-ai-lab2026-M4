using System.Net.Http.Json;
using SsoAdmin.Web;

namespace SsoAdmin.Test.TestFixtures;

/// <summary>Fábrica de integración para el host <c>SsoAdmin.Web</c> sobre SQLite.</summary>
public class WebTestFactory : SqliteWebApplicationFactory<WebHostMarker>
{
    /// <summary>Crea un cliente autenticado con la cuenta <c>admin</c> precargada (US2).</summary>
    public async Task<HttpClient> CrearClienteAdminAsync()
    {
        HttpClient client = CreateClient();
        HttpResponseMessage login = await client.PostAsJsonAsync("/api/auth/login",
            new { usuario = "admin", password = "admin" });
        login.EnsureSuccessStatusCode();
        return client;
    }
}

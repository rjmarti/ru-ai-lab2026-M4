using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using SsoAdmin.API;

namespace SsoAdmin.Test.TestFixtures;

/// <summary>Fábrica de integración para el host <c>SsoAdmin.API</c> sobre SQLite.</summary>
public class ApiTestFactory : SqliteWebApplicationFactory<ApiHostMarker>
{
    /// <summary>Clave de API configurada para los tests (FR-016).</summary>
    public const string ApiKey = "test-api-key-123";

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SsoApiKey:Value"] = ApiKey
            }));
    }
}

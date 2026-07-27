using SsoAdmin.API;

namespace SsoAdmin.Test.TestFixtures;

/// <summary>Fábrica de integración para el host <c>SsoAdmin.API</c> sobre SQLite.</summary>
public class ApiTestFactory : SqliteWebApplicationFactory<ApiHostMarker>
{
}

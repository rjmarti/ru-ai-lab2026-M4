using SsoAdmin.Web;

namespace SsoAdmin.Test.TestFixtures;

/// <summary>Fábrica de integración para el host <c>SsoAdmin.Web</c> sobre SQLite.</summary>
public class WebTestFactory : SqliteWebApplicationFactory<WebHostMarker>
{
}

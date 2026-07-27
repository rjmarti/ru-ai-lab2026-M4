using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace SsoAdmin.API.Auth;

/// <summary>Opciones del esquema de autenticación por API key (sin parámetros propios).</summary>
public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>Nombre del esquema de autenticación.</summary>
    public const string SchemeName = "ApiKey";

    /// <summary>Nombre del header que transporta la clave de API.</summary>
    public const string HeaderName = "X-Api-Key";
}

/// <summary>
/// Esquema de autenticación que exige el header <c>X-Api-Key</c> y lo compara, en tiempo
/// constante, contra el valor configurado (FR-016). Toda solicitud sin clave o con clave
/// inválida se rechaza con <c>401 Unauthorized</c> antes de evaluar la credencial.
/// </summary>
public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly SsoApiKeyOptions _apiKeyOptions;

    /// <summary>Crea el handler con las dependencias del pipeline de autenticación.</summary>
    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<SsoApiKeyOptions> apiKeyOptions)
        : base(options, logger, encoder)
    {
        _apiKeyOptions = apiKeyOptions.Value;
    }

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthenticationOptions.HeaderName, out var header) ||
            string.IsNullOrEmpty(header))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string configurada = _apiKeyOptions.Value;
        if (string.IsNullOrEmpty(configurada) || !ClavesCoinciden(header.ToString(), configurada))
        {
            return Task.FromResult(AuthenticateResult.Fail("Clave de API inválida."));
        }

        Claim[] claims = [new Claim(ClaimTypes.Name, "sso-client")];
        ClaimsIdentity identity = new(claims, Scheme.Name);
        AuthenticationTicket ticket = new(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static bool ClavesCoinciden(string recibida, string esperada)
    {
        byte[] recibidaBytes = Encoding.UTF8.GetBytes(recibida);
        byte[] esperadaBytes = Encoding.UTF8.GetBytes(esperada);
        return CryptographicOperations.FixedTimeEquals(recibidaBytes, esperadaBytes);
    }
}

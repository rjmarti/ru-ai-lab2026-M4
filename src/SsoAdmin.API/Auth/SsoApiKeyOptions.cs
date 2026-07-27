namespace SsoAdmin.API.Auth;

/// <summary>
/// Valor de la clave de API del endpoint SSO, provisto de forma externalizada
/// (Principio II) desde la sección de configuración <c>SsoApiKey</c>.
/// </summary>
public sealed class SsoApiKeyOptions
{
    /// <summary>Nombre de la sección de configuración que enlaza estas opciones.</summary>
    public const string SectionName = "SsoApiKey";

    /// <summary>Valor esperado del header <c>X-Api-Key</c> (FR-016).</summary>
    public string Value { get; set; } = string.Empty;
}

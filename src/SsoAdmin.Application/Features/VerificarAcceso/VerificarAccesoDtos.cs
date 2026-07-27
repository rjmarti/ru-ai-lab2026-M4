using System.Text.Json.Serialization;

namespace SsoAdmin.Application.Features.VerificarAcceso;

/// <summary>Motivos posibles de denegación de acceso devueltos por el endpoint SSO (FR-008).</summary>
public static class MotivoAcceso
{
    /// <summary>La combinación username+emisor no existe.</summary>
    public const string CredencialNoEncontrada = "credencial_no_encontrada";

    /// <summary>El usuario dueño de la credencial está dado de baja.</summary>
    public const string UsuarioInactivo = "usuario_inactivo";

    /// <summary>La URL no corresponde a ninguna aplicación registrada.</summary>
    public const string AplicacionNoEncontrada = "aplicacion_no_encontrada";

    /// <summary>No existe un permiso vigente (incluye fecha_desde futura).</summary>
    public const string PermisoNoEncontrado = "permiso_no_encontrado";

    /// <summary>El permiso existente ya expiró.</summary>
    public const string PermisoVencido = "permiso_vencido";
}

/// <summary>Solicitud de verificación de acceso enviada por el SSO externo.</summary>
/// <param name="Username">Nombre de usuario de la credencial. Requerido.</param>
/// <param name="Emisor">Proveedor de identidad emisor de la credencial. Requerido.</param>
/// <param name="AplicacionUrl">URL de la aplicación a verificar. Requerido.</param>
public sealed record VerificarAccesoRequest(string Username, string Emisor, string AplicacionUrl);

/// <summary>Respuesta de verificación de acceso.</summary>
public sealed class VerificarAccesoResponse
{
    /// <summary>Indica si la credencial tiene acceso vigente a la aplicación.</summary>
    public bool Allowed { get; init; }

    /// <summary>Motivo de la denegación; presente únicamente cuando <see cref="Allowed"/> es <c>false</c>.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Motivo { get; init; }

    /// <summary>Crea una respuesta de acceso permitido.</summary>
    public static VerificarAccesoResponse Permitido() => new() { Allowed = true };

    /// <summary>Crea una respuesta de acceso denegado con el motivo indicado.</summary>
    public static VerificarAccesoResponse Denegado(string motivo) => new() { Allowed = false, Motivo = motivo };
}

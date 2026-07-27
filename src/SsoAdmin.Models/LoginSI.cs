namespace SsoAdmin.Models;

/// <summary>
/// Cuenta de un miembro de Seguridad Informática (SI) que administra el sistema.
/// Entidad separada de <see cref="Usuario"/>; no participa de las verificaciones del SSO.
/// La contraseña se almacena como hash no reversible (FR-007).
/// </summary>
public class LoginSI
{
    /// <summary>Identificador único generado por el sistema.</summary>
    public Guid Id { get; set; }

    /// <summary>Nombre de usuario de SI. Obligatorio, único.</summary>
    public string Usuario { get; set; } = string.Empty;

    /// <summary>Hash no reversible de la contraseña (PasswordHasher, FR-007).</summary>
    public string PasswordHash { get; set; } = string.Empty;
}

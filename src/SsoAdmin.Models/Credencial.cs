namespace SsoAdmin.Models;

/// <summary>
/// Identifica a un <see cref="Usuario"/> ante un proveedor de identidad externo.
/// La combinación <see cref="Username"/> + <see cref="Emisor"/> es única en todo el
/// sistema (FR-001) y no contiene contraseñas ni derivados (FR-013).
/// </summary>
public class Credencial
{
    /// <summary>Identificador único generado por el sistema.</summary>
    public Guid Id { get; set; }

    /// <summary>Usuario dueño de la credencial. Obligatorio.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Nombre de usuario ante el proveedor de identidad. Obligatorio.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Proveedor de identidad que emite la credencial. Obligatorio.</summary>
    public string Emisor { get; set; } = string.Empty;

    /// <summary>Usuario asociado (navegación).</summary>
    public Usuario? Usuario { get; set; }
}

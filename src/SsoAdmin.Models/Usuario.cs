namespace SsoAdmin.Models;

/// <summary>
/// Persona a la que se le administra el acceso a aplicaciones a través del SSO.
/// Es una entidad de negocio distinta de <see cref="LoginSI"/> (la cuenta de SI).
/// </summary>
public class Usuario
{
    /// <summary>Identificador único generado por el sistema.</summary>
    public Guid Id { get; set; }

    /// <summary>Nombre del usuario. Obligatorio, no vacío (FR-010).</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Estado del usuario. <c>true</c> = activo; <c>false</c> = baja lógica (FR-006).
    /// La baja caduca en cascada todos los permisos activos del usuario.
    /// </summary>
    public bool Activo { get; set; } = true;

    /// <summary>Credenciales asociadas al usuario (1..N, emisores distintos).</summary>
    public ICollection<Credencial> Credenciales { get; set; } = new List<Credencial>();

    /// <summary>Permisos de acceso del usuario a aplicaciones (0..N).</summary>
    public ICollection<PermisoAcceso> Permisos { get; set; } = new List<PermisoAcceso>();
}

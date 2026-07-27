namespace SsoAdmin.Models;

/// <summary>
/// Sistema externo cuyo acceso es controlado por el SSO. La <see cref="Url"/> se usa
/// como identificador de consulta por el SSO (FR-008).
/// </summary>
public class Aplicacion
{
    /// <summary>Identificador único generado por el sistema.</summary>
    public Guid Id { get; set; }

    /// <summary>Nombre de la aplicación. Obligatorio.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>URL de la aplicación. Obligatoria, no vacía (FR-003).</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Permisos de acceso otorgados sobre esta aplicación (0..N).</summary>
    public ICollection<PermisoAcceso> Permisos { get; set; } = new List<PermisoAcceso>();
}

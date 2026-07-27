namespace SsoAdmin.Models;

/// <summary>
/// Vigencia de acceso de un <see cref="Usuario"/> a una <see cref="Aplicacion"/>.
/// Para un mismo usuario y aplicación los períodos no pueden solaparse (FR-004).
/// </summary>
public class PermisoAcceso
{
    /// <summary>Identificador único generado por el sistema.</summary>
    public Guid Id { get; set; }

    /// <summary>Usuario al que se le otorga el permiso. Obligatorio.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Aplicación sobre la que se otorga el permiso. Obligatorio.</summary>
    public Guid AplicacionId { get; set; }

    /// <summary>Fecha de inicio de vigencia. Obligatoria (FR-004).</summary>
    public DateOnly FechaDesde { get; set; }

    /// <summary>
    /// Fecha de fin de vigencia. Opcional; <c>null</c> significa vigencia indefinida (FR-004).
    /// </summary>
    public DateOnly? FechaHasta { get; set; }

    /// <summary>Usuario asociado (navegación).</summary>
    public Usuario? Usuario { get; set; }

    /// <summary>Aplicación asociada (navegación).</summary>
    public Aplicacion? Aplicacion { get; set; }
}

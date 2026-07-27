using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SsoAdmin.Models;

namespace SsoAdmin.Data.Configurations;

/// <summary>
/// Configuración EF Core de la entidad <see cref="PermisoAcceso"/>. El no solapamiento
/// de períodos es una regla de rango que se verifica transaccionalmente (research.md #5);
/// aquí se define un índice de soporte sobre <c>(UsuarioId, AplicacionId)</c>.
/// </summary>
public class PermisoAccesoConfiguration : IEntityTypeConfiguration<PermisoAcceso>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PermisoAcceso> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.FechaDesde).IsRequired();
        builder.Property(p => p.FechaHasta);

        builder.HasOne(p => p.Aplicacion)
            .WithMany(a => a.Permisos)
            .HasForeignKey(p => p.AplicacionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.UsuarioId, p.AplicacionId })
            .HasDatabaseName("IX_PermisoAcceso_Usuario_Aplicacion");
    }
}

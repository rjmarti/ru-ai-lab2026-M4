using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SsoAdmin.Models;

namespace SsoAdmin.Data.Configurations;

/// <summary>Configuración EF Core de la entidad <see cref="Aplicacion"/>.</summary>
public class AplicacionConfiguration : IEntityTypeConfiguration<Aplicacion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Aplicacion> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Nombre).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Url).IsRequired().HasMaxLength(2048);

        builder.HasIndex(a => a.Url).HasDatabaseName("IX_Aplicacion_Url");
    }
}

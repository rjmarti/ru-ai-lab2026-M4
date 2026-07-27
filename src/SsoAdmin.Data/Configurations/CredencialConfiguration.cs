using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SsoAdmin.Models;

namespace SsoAdmin.Data.Configurations;

/// <summary>
/// Configuración EF Core de la entidad <see cref="Credencial"/>. Define el índice único
/// compuesto <c>(Username, Emisor)</c> que garantiza la unicidad global incluso bajo
/// concurrencia (FR-001/FR-002, research.md #4).
/// </summary>
public class CredencialConfiguration : IEntityTypeConfiguration<Credencial>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Credencial> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Username).IsRequired().HasMaxLength(256);
        builder.Property(c => c.Emisor).IsRequired().HasMaxLength(256);

        builder.HasIndex(c => new { c.Username, c.Emisor })
            .IsUnique()
            .HasDatabaseName("UX_Credencial_Username_Emisor");
    }
}

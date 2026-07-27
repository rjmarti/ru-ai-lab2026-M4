using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SsoAdmin.Models;

namespace SsoAdmin.Data.Configurations;

/// <summary>Configuración EF Core de la entidad <see cref="Usuario"/>.</summary>
public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Nombre).IsRequired().HasMaxLength(200);
        builder.Property(u => u.Activo).IsRequired().HasDefaultValue(true);

        builder.HasMany(u => u.Credenciales)
            .WithOne(c => c.Usuario!)
            .HasForeignKey(c => c.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Permisos)
            .WithOne(p => p.Usuario!)
            .HasForeignKey(p => p.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

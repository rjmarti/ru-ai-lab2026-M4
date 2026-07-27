using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SsoAdmin.Models;

namespace SsoAdmin.Data.Configurations;

/// <summary>Configuración EF Core de la entidad <see cref="LoginSI"/>.</summary>
public class LoginSIConfiguration : IEntityTypeConfiguration<LoginSI>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LoginSI> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Usuario).IsRequired().HasMaxLength(256);
        builder.Property(l => l.PasswordHash).IsRequired();

        builder.HasIndex(l => l.Usuario)
            .IsUnique()
            .HasDatabaseName("UX_LoginSI_Usuario");
    }
}

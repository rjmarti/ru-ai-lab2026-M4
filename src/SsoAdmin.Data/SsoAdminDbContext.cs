using Microsoft.EntityFrameworkCore;
using SsoAdmin.Models;

namespace SsoAdmin.Data;

/// <summary>
/// Contexto de Entity Framework Core para el backend de administración del SSO.
/// Mapea las entidades de dominio y aplica sus configuraciones (índices únicos,
/// relaciones) desde el ensamblado actual.
/// </summary>
public class SsoAdminDbContext : DbContext
{
    /// <summary>Crea el contexto con las opciones provistas por DI.</summary>
    public SsoAdminDbContext(DbContextOptions<SsoAdminDbContext> options) : base(options)
    {
    }

    /// <summary>Usuarios administrados a través del SSO.</summary>
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    /// <summary>Credenciales asociadas a los usuarios.</summary>
    public DbSet<Credencial> Credenciales => Set<Credencial>();

    /// <summary>Aplicaciones cuyo acceso controla el SSO.</summary>
    public DbSet<Aplicacion> Aplicaciones => Set<Aplicacion>();

    /// <summary>Permisos de acceso de usuarios a aplicaciones.</summary>
    public DbSet<PermisoAcceso> Permisos => Set<PermisoAcceso>();

    /// <summary>Cuentas de login de Seguridad Informática.</summary>
    public DbSet<LoginSI> LoginsSI => Set<LoginSI>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SsoAdminDbContext).Assembly);
    }
}

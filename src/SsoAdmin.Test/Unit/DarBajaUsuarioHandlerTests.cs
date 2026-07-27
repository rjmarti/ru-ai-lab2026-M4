using SsoAdmin.Application.Common;
using SsoAdmin.Application.Features.GestionUsuarios;
using SsoAdmin.Models;
using SsoAdmin.Test.Fakes;

namespace SsoAdmin.Test.Unit;

/// <summary>
/// Tests unitarios de <see cref="DarBajaUsuarioHandler"/>: la baja lógica caduca todos los
/// permisos activos del usuario en la misma operación (FR-006/FR-015).
/// </summary>
public class DarBajaUsuarioHandlerTests
{
    private static readonly DateOnly Hoy = new(2026, 07, 27);

    [Fact]
    public async Task Baja_caduca_todos_los_permisos_activos_y_desactiva_al_usuario()
    {
        Usuario usuario = new() { Id = Guid.NewGuid(), Nombre = "Ana", Activo = true };
        PermisoAcceso indefinido = new() { Id = Guid.NewGuid(), UsuarioId = usuario.Id, FechaDesde = Hoy.AddDays(-10), FechaHasta = null };
        PermisoAcceso conFin = new() { Id = Guid.NewGuid(), UsuarioId = usuario.Id, FechaDesde = Hoy.AddDays(-10), FechaHasta = Hoy.AddDays(30) };
        usuario.Permisos.Add(indefinido);
        usuario.Permisos.Add(conFin);

        FakeUsuarioRepository repo = new(usuario);
        DarBajaUsuarioHandler handler = new(repo, new FixedTimeProvider(Hoy));

        Result<bool> result = await handler.HandleAsync(usuario.Id);

        Assert.True(result.IsSuccess);
        Assert.False(usuario.Activo);
        Assert.Equal(Hoy, indefinido.FechaHasta);
        Assert.Equal(Hoy, conFin.FechaHasta);
        Assert.True(repo.GuardoCambios);
    }

    [Fact]
    public async Task Baja_no_altera_permisos_ya_vencidos()
    {
        Usuario usuario = new() { Id = Guid.NewGuid(), Nombre = "Ana", Activo = true };
        DateOnly finPasado = Hoy.AddDays(-5);
        PermisoAcceso vencido = new() { Id = Guid.NewGuid(), UsuarioId = usuario.Id, FechaDesde = Hoy.AddDays(-10), FechaHasta = finPasado };
        usuario.Permisos.Add(vencido);

        FakeUsuarioRepository repo = new(usuario);
        DarBajaUsuarioHandler handler = new(repo, new FixedTimeProvider(Hoy));

        await handler.HandleAsync(usuario.Id);

        Assert.Equal(finPasado, vencido.FechaHasta);
    }

    [Fact]
    public async Task Baja_de_usuario_inexistente_devuelve_not_found()
    {
        FakeUsuarioRepository repo = new(null);
        DarBajaUsuarioHandler handler = new(repo, new FixedTimeProvider(Hoy));

        Result<bool> result = await handler.HandleAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
    }
}

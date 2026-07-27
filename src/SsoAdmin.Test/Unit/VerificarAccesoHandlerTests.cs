using Microsoft.Extensions.Logging.Abstractions;
using SsoAdmin.Application.Features.VerificarAcceso;
using SsoAdmin.Data.Repositories;
using SsoAdmin.Models;
using SsoAdmin.Test.Fakes;

namespace SsoAdmin.Test.Unit;

/// <summary>
/// Tests unitarios de <see cref="VerificarAccesoHandler"/>: precedencia de motivo y reglas
/// de negocio sin HTTP ni I/O real (US1, FR-008).
/// </summary>
public class VerificarAccesoHandlerTests
{
    private static readonly DateOnly Hoy = new(2026, 07, 27);

    private static VerificarAccesoHandler CrearHandler(
        FakeCredencialRepository credenciales,
        FakeAplicacionRepository aplicaciones,
        FakePermisoAccesoRepository permisos) =>
        new(credenciales, aplicaciones, permisos, new FixedTimeProvider(Hoy),
            NullLogger<VerificarAccesoHandler>.Instance);

    private static VerificarAccesoRequest Request() => new("u1", "google", "https://app.test");

    [Fact]
    public async Task Credencial_inexistente_devuelve_credencial_no_encontrada()
    {
        VerificarAccesoHandler handler = CrearHandler(new FakeCredencialRepository(null), new FakeAplicacionRepository(null), new FakePermisoAccesoRepository([]));

        VerificarAccesoResponse response = await handler.HandleAsync(Request());

        Assert.False(response.Allowed);
        Assert.Equal(MotivoAcceso.CredencialNoEncontrada, response.Motivo);
    }

    [Fact]
    public async Task Usuario_inactivo_devuelve_usuario_inactivo()
    {
        Usuario usuario = new() { Id = Guid.NewGuid(), Nombre = "Ana", Activo = false };
        Credencial credencial = new() { Id = Guid.NewGuid(), UsuarioId = usuario.Id, Username = "u1", Emisor = "google", Usuario = usuario };

        VerificarAccesoHandler handler = CrearHandler(new FakeCredencialRepository(credencial), new FakeAplicacionRepository(null), new FakePermisoAccesoRepository([]));

        VerificarAccesoResponse response = await handler.HandleAsync(Request());

        Assert.False(response.Allowed);
        Assert.Equal(MotivoAcceso.UsuarioInactivo, response.Motivo);
    }

    [Fact]
    public async Task Aplicacion_inexistente_devuelve_aplicacion_no_encontrada()
    {
        (Credencial credencial, _) = CredencialActiva();

        VerificarAccesoHandler handler = CrearHandler(new FakeCredencialRepository(credencial), new FakeAplicacionRepository(null), new FakePermisoAccesoRepository([]));

        VerificarAccesoResponse response = await handler.HandleAsync(Request());

        Assert.False(response.Allowed);
        Assert.Equal(MotivoAcceso.AplicacionNoEncontrada, response.Motivo);
    }

    [Fact]
    public async Task Sin_permiso_devuelve_permiso_no_encontrado()
    {
        (Credencial credencial, Usuario usuario) = CredencialActiva();
        Aplicacion app = App();

        VerificarAccesoHandler handler = CrearHandler(new FakeCredencialRepository(credencial), new FakeAplicacionRepository(app), new FakePermisoAccesoRepository([]));

        VerificarAccesoResponse response = await handler.HandleAsync(Request());

        Assert.False(response.Allowed);
        Assert.Equal(MotivoAcceso.PermisoNoEncontrado, response.Motivo);
    }

    [Fact]
    public async Task Permiso_con_fecha_desde_futura_devuelve_permiso_no_encontrado()
    {
        (Credencial credencial, Usuario usuario) = CredencialActiva();
        Aplicacion app = App();
        PermisoAcceso futuro = new() { Id = Guid.NewGuid(), UsuarioId = usuario.Id, AplicacionId = app.Id, FechaDesde = Hoy.AddDays(5) };

        VerificarAccesoHandler handler = CrearHandler(new FakeCredencialRepository(credencial), new FakeAplicacionRepository(app), new FakePermisoAccesoRepository([futuro]));

        VerificarAccesoResponse response = await handler.HandleAsync(Request());

        Assert.False(response.Allowed);
        Assert.Equal(MotivoAcceso.PermisoNoEncontrado, response.Motivo);
    }

    [Fact]
    public async Task Permiso_vencido_devuelve_permiso_vencido()
    {
        (Credencial credencial, Usuario usuario) = CredencialActiva();
        Aplicacion app = App();
        PermisoAcceso vencido = new() { Id = Guid.NewGuid(), UsuarioId = usuario.Id, AplicacionId = app.Id, FechaDesde = Hoy.AddDays(-30), FechaHasta = Hoy.AddDays(-1) };

        VerificarAccesoHandler handler = CrearHandler(new FakeCredencialRepository(credencial), new FakeAplicacionRepository(app), new FakePermisoAccesoRepository([vencido]));

        VerificarAccesoResponse response = await handler.HandleAsync(Request());

        Assert.False(response.Allowed);
        Assert.Equal(MotivoAcceso.PermisoVencido, response.Motivo);
    }

    [Fact]
    public async Task Permiso_vigente_indefinido_devuelve_allowed()
    {
        (Credencial credencial, Usuario usuario) = CredencialActiva();
        Aplicacion app = App();
        PermisoAcceso vigente = new() { Id = Guid.NewGuid(), UsuarioId = usuario.Id, AplicacionId = app.Id, FechaDesde = Hoy.AddDays(-1), FechaHasta = null };

        VerificarAccesoHandler handler = CrearHandler(new FakeCredencialRepository(credencial), new FakeAplicacionRepository(app), new FakePermisoAccesoRepository([vigente]));

        VerificarAccesoResponse response = await handler.HandleAsync(Request());

        Assert.True(response.Allowed);
        Assert.Null(response.Motivo);
    }

    private static (Credencial, Usuario) CredencialActiva()
    {
        Usuario usuario = new() { Id = Guid.NewGuid(), Nombre = "Ana", Activo = true };
        Credencial credencial = new() { Id = Guid.NewGuid(), UsuarioId = usuario.Id, Username = "u1", Emisor = "google", Usuario = usuario };
        return (credencial, usuario);
    }

    private static Aplicacion App() => new() { Id = Guid.NewGuid(), Nombre = "App", Url = "https://app.test" };
}

using SsoAdmin.Data.Repositories;
using SsoAdmin.Models;

namespace SsoAdmin.Test.Fakes;

/// <summary><see cref="TimeProvider"/> que devuelve una fecha fija para tests deterministas.</summary>
public sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;

    /// <summary>Crea el proveedor fijado en la fecha indicada (a medianoche UTC).</summary>
    public FixedTimeProvider(DateOnly hoy) =>
        _now = new DateTimeOffset(hoy.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => _now;
}

/// <summary>Fake de <see cref="IUsuarioRepository"/> para tests unitarios.</summary>
public sealed class FakeUsuarioRepository(Usuario? usuario) : IUsuarioRepository
{
    /// <summary>Indica si se persistieron cambios (para asserts).</summary>
    public bool GuardoCambios { get; private set; }

    /// <inheritdoc />
    public Task<Usuario?> ObtenerConPermisosAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(usuario);

    /// <inheritdoc />
    public Task<Usuario?> ObtenerAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(usuario);

    /// <inheritdoc />
    public Task GuardarCambiosAsync(CancellationToken cancellationToken = default)
    {
        GuardoCambios = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

/// <summary>Fake de <see cref="ICredencialRepository"/> para tests unitarios.</summary>
public sealed class FakeCredencialRepository(Credencial? credencial) : ICredencialRepository
{
    /// <inheritdoc />
    public Task<Credencial?> ObtenerPorUsernameEmisorAsync(string username, string emisor, CancellationToken cancellationToken = default) =>
        Task.FromResult(credencial);

    /// <inheritdoc />
    public Task<IReadOnlyList<Credencial>> ListarConUsuarioAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public Task<bool> IntentarCrearAsync(Credencial credencial, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public Task<bool> EliminarAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

/// <summary>
/// Fake de <see cref="ICredencialRepository"/> que lanza una excepción al consultar, para
/// verificar que el endpoint SSO responde <c>500 Internal Server Error</c> (FR-009).
/// </summary>
public sealed class FaultingCredencialRepository : ICredencialRepository
{
    /// <inheritdoc />
    public Task<Credencial?> ObtenerPorUsernameEmisorAsync(string username, string emisor, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Fallo inesperado simulado.");

    /// <inheritdoc />
    public Task<IReadOnlyList<Credencial>> ListarConUsuarioAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public Task<bool> IntentarCrearAsync(Credencial credencial, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public Task<bool> EliminarAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

/// <summary>Fake de <see cref="IAplicacionRepository"/> para tests unitarios.</summary>
public sealed class FakeAplicacionRepository(Aplicacion? aplicacion) : IAplicacionRepository
{
    /// <inheritdoc />
    public Task<Aplicacion?> ObtenerPorUrlAsync(string url, CancellationToken cancellationToken = default) =>
        Task.FromResult(aplicacion);

    /// <inheritdoc />
    public Task<IReadOnlyList<Aplicacion>> ListarAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public Task<Aplicacion?> ObtenerAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public Task AgregarAsync(Aplicacion aplicacion, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public Task<bool> EliminarAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public Task GuardarCambiosAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

/// <summary>Fake de <see cref="IPermisoAccesoRepository"/> para tests unitarios.</summary>
public sealed class FakePermisoAccesoRepository(IReadOnlyList<PermisoAcceso> permisos) : IPermisoAccesoRepository
{
    /// <inheritdoc />
    public Task<IReadOnlyList<PermisoAcceso>> ListarPorUsuarioAplicacionAsync(Guid usuarioId, Guid aplicacionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(permisos);

    /// <inheritdoc />
    public Task<IReadOnlyList<PermisoAcceso>> ListarAsync(Guid? usuarioId, Guid? aplicacionId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public Task<PermisoAcceso?> ObtenerAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public Task<ResultadoOtorgarPermiso> OtorgarAsync(PermisoAcceso permiso, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public Task GuardarCambiosAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

using Microsoft.Extensions.Logging;
using SsoAdmin.Data.Repositories;
using SsoAdmin.Models;

namespace SsoAdmin.Application.Features.VerificarAcceso;

/// <summary>
/// Resuelve si una credencial tiene acceso vigente a una aplicación, devolviendo
/// <c>allowed</c> y, en caso negativo, el <c>motivo</c> según la precedencia del contrato
/// (US1, FR-008): credencial → usuario → aplicación → permiso.
/// </summary>
public sealed class VerificarAccesoHandler
{
    private readonly ICredencialRepository _credenciales;
    private readonly IAplicacionRepository _aplicaciones;
    private readonly IPermisoAccesoRepository _permisos;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<VerificarAccesoHandler> _logger;

    /// <summary>Crea el handler con las dependencias inyectadas.</summary>
    public VerificarAccesoHandler(
        ICredencialRepository credenciales,
        IAplicacionRepository aplicaciones,
        IPermisoAccesoRepository permisos,
        TimeProvider timeProvider,
        ILogger<VerificarAccesoHandler> logger)
    {
        _credenciales = credenciales;
        _aplicaciones = aplicaciones;
        _permisos = permisos;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>Evalúa la solicitud de verificación y devuelve el resultado de acceso.</summary>
    public async Task<VerificarAccesoResponse> HandleAsync(VerificarAccesoRequest request, CancellationToken cancellationToken = default)
    {
        Credencial? credencial = await _credenciales.ObtenerPorUsernameEmisorAsync(request.Username, request.Emisor, cancellationToken);
        if (credencial is null)
        {
            return VerificarAccesoResponse.Denegado(MotivoAcceso.CredencialNoEncontrada);
        }

        if (credencial.Usuario is null || !credencial.Usuario.Activo)
        {
            return VerificarAccesoResponse.Denegado(MotivoAcceso.UsuarioInactivo);
        }

        Aplicacion? aplicacion = await _aplicaciones.ObtenerPorUrlAsync(request.AplicacionUrl, cancellationToken);
        if (aplicacion is null)
        {
            return VerificarAccesoResponse.Denegado(MotivoAcceso.AplicacionNoEncontrada);
        }

        IReadOnlyList<PermisoAcceso> permisos =
            await _permisos.ListarPorUsuarioAplicacionAsync(credencial.UsuarioId, aplicacion.Id, cancellationToken);

        DateOnly hoy = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

        bool hayVigente = permisos.Any(p => p.FechaDesde <= hoy && (p.FechaHasta is null || p.FechaHasta >= hoy));
        if (hayVigente)
        {
            return VerificarAccesoResponse.Permitido();
        }

        bool hayVencido = permisos.Any(p => p.FechaHasta is not null && p.FechaHasta < hoy);
        string motivo = hayVencido ? MotivoAcceso.PermisoVencido : MotivoAcceso.PermisoNoEncontrado;

        _logger.LogInformation(
            "Acceso denegado para credencial {Username}/{Emisor} a {Url}: {Motivo}",
            request.Username, request.Emisor, request.AplicacionUrl, motivo);

        return VerificarAccesoResponse.Denegado(motivo);
    }
}

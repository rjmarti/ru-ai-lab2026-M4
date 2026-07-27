using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SsoAdmin.Application.Features.GestionPermisos;
using SsoAdmin.Web.Common;

namespace SsoAdmin.Web.Controllers;

/// <summary>API interna de gestión de permisos (US4, FR-004/FR-005). Requiere cookie de SI.</summary>
[ApiController]
[Route("api/permisos")]
[Authorize]
public sealed class PermisosController : ControllerBase
{
    private readonly ListarPermisosHandler _listar;
    private readonly OtorgarPermisoHandler _otorgar;
    private readonly RevocarPermisoHandler _revocar;
    private readonly IValidator<OtorgarPermisoRequest> _otorgarValidator;

    /// <summary>Crea el controller con sus dependencias inyectadas.</summary>
    public PermisosController(
        ListarPermisosHandler listar,
        OtorgarPermisoHandler otorgar,
        RevocarPermisoHandler revocar,
        IValidator<OtorgarPermisoRequest> otorgarValidator)
    {
        _listar = listar;
        _otorgar = otorgar;
        _revocar = revocar;
        _otorgarValidator = otorgarValidator;
    }

    /// <summary>Lista permisos con filtros opcionales por usuario y aplicación.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] Guid? usuarioId, [FromQuery] Guid? aplicacionId, CancellationToken cancellationToken) =>
        Ok(await _listar.HandleAsync(usuarioId, aplicacionId, cancellationToken));

    /// <summary>Otorga un permiso de acceso.</summary>
    [HttpPost]
    public async Task<IActionResult> Otorgar([FromBody] OtorgarPermisoRequest request, CancellationToken cancellationToken)
    {
        FluentValidation.Results.ValidationResult validation = await _otorgarValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { errores = validation.Errors.Select(e => e.ErrorMessage) });
        }

        return (await _otorgar.HandleAsync(request, cancellationToken)).ToActionResult(this);
    }

    /// <summary>Revoca un permiso, fijando su fecha de fin en la fecha actual.</summary>
    [HttpPost("{id:guid}/revocar")]
    public async Task<IActionResult> Revocar(Guid id, CancellationToken cancellationToken) =>
        (await _revocar.HandleAsync(id, cancellationToken)).ToActionResult(this);
}

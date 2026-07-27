using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SsoAdmin.Application.Features.GestionCredenciales;
using SsoAdmin.Web.Common;

namespace SsoAdmin.Web.Controllers;

/// <summary>API interna de gestión de credenciales (US3, FR-011). Requiere cookie de SI.</summary>
[ApiController]
[Route("api/credenciales")]
[Authorize]
public sealed class CredencialesController : ControllerBase
{
    private readonly ListarCredencialesHandler _listar;
    private readonly CrearCredencialHandler _crear;
    private readonly EliminarCredencialHandler _eliminar;
    private readonly IValidator<CrearCredencialRequest> _crearValidator;

    /// <summary>Crea el controller con sus dependencias inyectadas.</summary>
    public CredencialesController(
        ListarCredencialesHandler listar,
        CrearCredencialHandler crear,
        EliminarCredencialHandler eliminar,
        IValidator<CrearCredencialRequest> crearValidator)
    {
        _listar = listar;
        _crear = crear;
        _eliminar = eliminar;
        _crearValidator = crearValidator;
    }

    /// <summary>Lista todas las credenciales con su usuario asociado.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken) =>
        Ok(await _listar.HandleAsync(cancellationToken));

    /// <summary>Crea una nueva credencial.</summary>
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearCredencialRequest request, CancellationToken cancellationToken)
    {
        FluentValidation.Results.ValidationResult validation = await _crearValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { errores = validation.Errors.Select(e => e.ErrorMessage) });
        }

        return (await _crear.HandleAsync(request, cancellationToken)).ToActionResult(this);
    }

    /// <summary>Elimina físicamente una credencial.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken) =>
        (await _eliminar.HandleAsync(id, cancellationToken)).ToActionResult(this);
}

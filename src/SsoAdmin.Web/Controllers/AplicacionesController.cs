using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SsoAdmin.Application.Features.GestionAplicaciones;
using SsoAdmin.Web.Common;

namespace SsoAdmin.Web.Controllers;

/// <summary>API interna de gestión de aplicaciones (US4, FR-012). Requiere cookie de SI.</summary>
[ApiController]
[Route("api/aplicaciones")]
[Authorize]
public sealed class AplicacionesController : ControllerBase
{
    private readonly ListarAplicacionesHandler _listar;
    private readonly CrearAplicacionHandler _crear;
    private readonly EditarAplicacionHandler _editar;
    private readonly EliminarAplicacionHandler _eliminar;
    private readonly IValidator<CrearAplicacionRequest> _crearValidator;
    private readonly IValidator<EditarAplicacionRequest> _editarValidator;

    /// <summary>Crea el controller con sus dependencias inyectadas.</summary>
    public AplicacionesController(
        ListarAplicacionesHandler listar,
        CrearAplicacionHandler crear,
        EditarAplicacionHandler editar,
        EliminarAplicacionHandler eliminar,
        IValidator<CrearAplicacionRequest> crearValidator,
        IValidator<EditarAplicacionRequest> editarValidator)
    {
        _listar = listar;
        _crear = crear;
        _editar = editar;
        _eliminar = eliminar;
        _crearValidator = crearValidator;
        _editarValidator = editarValidator;
    }

    /// <summary>Lista todas las aplicaciones.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken) =>
        Ok(await _listar.HandleAsync(cancellationToken));

    /// <summary>Registra una nueva aplicación.</summary>
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearAplicacionRequest request, CancellationToken cancellationToken)
    {
        FluentValidation.Results.ValidationResult validation = await _crearValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { errores = validation.Errors.Select(e => e.ErrorMessage) });
        }

        return (await _crear.HandleAsync(request, cancellationToken)).ToActionResult(this);
    }

    /// <summary>Edita nombre y URL de una aplicación.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Editar(Guid id, [FromBody] EditarAplicacionRequest request, CancellationToken cancellationToken)
    {
        FluentValidation.Results.ValidationResult validation = await _editarValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { errores = validation.Errors.Select(e => e.ErrorMessage) });
        }

        return (await _editar.HandleAsync(id, request, cancellationToken)).ToActionResult(this);
    }

    /// <summary>Elimina una aplicación.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken) =>
        (await _eliminar.HandleAsync(id, cancellationToken)).ToActionResult(this);
}

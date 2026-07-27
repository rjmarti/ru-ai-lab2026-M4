using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SsoAdmin.Application.Features.GestionUsuarios;
using SsoAdmin.Web.Common;

namespace SsoAdmin.Web.Controllers;

/// <summary>API interna de gestión de usuarios (US2, FR-010). Requiere cookie de SI.</summary>
[ApiController]
[Route("api/usuarios")]
[Authorize]
public sealed class UsuariosController : ControllerBase
{
    private readonly ListarUsuariosHandler _listar;
    private readonly CrearUsuarioHandler _crear;
    private readonly EditarUsuarioHandler _editar;
    private readonly DarBajaUsuarioHandler _darBaja;
    private readonly IValidator<CrearUsuarioRequest> _crearValidator;
    private readonly IValidator<EditarUsuarioRequest> _editarValidator;

    /// <summary>Crea el controller con sus dependencias inyectadas.</summary>
    public UsuariosController(
        ListarUsuariosHandler listar,
        CrearUsuarioHandler crear,
        EditarUsuarioHandler editar,
        DarBajaUsuarioHandler darBaja,
        IValidator<CrearUsuarioRequest> crearValidator,
        IValidator<EditarUsuarioRequest> editarValidator)
    {
        _listar = listar;
        _crear = crear;
        _editar = editar;
        _darBaja = darBaja;
        _crearValidator = crearValidator;
        _editarValidator = editarValidator;
    }

    /// <summary>Lista todos los usuarios con su estado.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken) =>
        Ok(await _listar.HandleAsync(cancellationToken));

    /// <summary>Crea un nuevo usuario.</summary>
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearUsuarioRequest request, CancellationToken cancellationToken)
    {
        FluentValidation.Results.ValidationResult validation = await _crearValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { errores = validation.Errors.Select(e => e.ErrorMessage) });
        }

        return (await _crear.HandleAsync(request, cancellationToken)).ToActionResult(this);
    }

    /// <summary>Edita el nombre de un usuario.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Editar(Guid id, [FromBody] EditarUsuarioRequest request, CancellationToken cancellationToken)
    {
        FluentValidation.Results.ValidationResult validation = await _editarValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { errores = validation.Errors.Select(e => e.ErrorMessage) });
        }

        return (await _editar.HandleAsync(id, request, cancellationToken)).ToActionResult(this);
    }

    /// <summary>Da de baja lógica al usuario, caducando sus permisos activos en cascada.</summary>
    [HttpPost("{id:guid}/baja")]
    public async Task<IActionResult> Baja(Guid id, CancellationToken cancellationToken) =>
        (await _darBaja.HandleAsync(id, cancellationToken)).ToActionResult(this);
}

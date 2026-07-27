using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SsoAdmin.API.Auth;
using SsoAdmin.Application.Features.VerificarAcceso;

namespace SsoAdmin.API.Controllers;

/// <summary>
/// Único endpoint externo consumido por el SSO. Protegido por API key (FR-016) y expone
/// <c>POST /api/sso/verificar</c> (US1, FR-008/FR-009).
/// </summary>
[ApiController]
[Route("api/sso")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationOptions.SchemeName)]
public sealed class SsoController : ControllerBase
{
    private readonly VerificarAccesoHandler _handler;
    private readonly IValidator<VerificarAccesoRequest> _validator;

    /// <summary>Crea el controller con sus dependencias inyectadas.</summary>
    public SsoController(VerificarAccesoHandler handler, IValidator<VerificarAccesoRequest> validator)
    {
        _handler = handler;
        _validator = validator;
    }

    /// <summary>
    /// Verifica si una credencial tiene acceso vigente a una aplicación. Devuelve
    /// <c>200 OK</c> con <c>allowed</c>/<c>motivo</c> para toda solicitud bien formada,
    /// <c>400</c> si falta un campo requerido y <c>401</c> si la API key es inválida.
    /// </summary>
    [HttpPost("verificar")]
    [ProducesResponseType(typeof(VerificarAccesoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Verificar([FromBody] VerificarAccesoRequest request, CancellationToken cancellationToken)
    {
        FluentValidation.Results.ValidationResult validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { errores = validation.Errors.Select(e => e.ErrorMessage) });
        }

        VerificarAccesoResponse response = await _handler.HandleAsync(request, cancellationToken);
        return Ok(response);
    }
}

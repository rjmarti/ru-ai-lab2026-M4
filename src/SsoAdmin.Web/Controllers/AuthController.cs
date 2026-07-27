using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SsoAdmin.Application.Common;
using SsoAdmin.Application.Features.AuthSI;

namespace SsoAdmin.Web.Controllers;

/// <summary>
/// API interna de autenticación de SI (US2). Emite/invalida la cookie de sesión del propio
/// host Web (same-origin con su JS, contracts/admin-api.md).
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly LoginHandler _handler;
    private readonly IValidator<LoginRequest> _validator;

    /// <summary>Crea el controller con sus dependencias inyectadas.</summary>
    public AuthController(LoginHandler handler, IValidator<LoginRequest> validator)
    {
        _handler = handler;
        _validator = validator;
    }

    /// <summary>Valida las credenciales de SI y, si son correctas, emite la cookie de sesión.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        FluentValidation.Results.ValidationResult validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { errores = validation.Errors.Select(e => e.ErrorMessage) });
        }

        Result<LoginResponse> result = await _handler.HandleAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return Unauthorized(new { error = result.Error!.Message });
        }

        Claim[] claims = [new Claim(ClaimTypes.Name, result.Value!.Usuario)];
        ClaimsIdentity identity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Ok(new { usuario = result.Value.Usuario });
    }

    /// <summary>Invalida la cookie de sesión.</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }
}

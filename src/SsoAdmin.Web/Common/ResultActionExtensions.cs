using Microsoft.AspNetCore.Mvc;
using SsoAdmin.Application.Common;

namespace SsoAdmin.Web.Common;

/// <summary>
/// Traduce un <see cref="Result{T}"/> de la capa de aplicación a un <see cref="IActionResult"/>,
/// mapeando cada <see cref="ErrorKind"/> a su código HTTP (400/401/404/409). Mantiene los
/// controllers libres de lógica de mapeo repetida.
/// </summary>
public static class ResultActionExtensions
{
    /// <summary>Convierte el resultado en <c>200 OK</c> con su valor, o en el error HTTP correspondiente.</summary>
    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller)
    {
        if (result.IsSuccess)
        {
            return controller.Ok(result.Value);
        }

        DomainError error = result.Error!;
        object cuerpo = new { error = error.Message };
        return error.Kind switch
        {
            ErrorKind.Validation => controller.BadRequest(cuerpo),
            ErrorKind.NotFound => controller.NotFound(cuerpo),
            ErrorKind.Conflict => controller.Conflict(cuerpo),
            ErrorKind.Unauthorized => controller.Unauthorized(cuerpo),
            _ => controller.StatusCode(StatusCodes.Status500InternalServerError, cuerpo)
        };
    }
}

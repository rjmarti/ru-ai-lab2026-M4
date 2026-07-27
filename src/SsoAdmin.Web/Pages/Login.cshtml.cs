using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SsoAdmin.Web.Pages;

/// <summary>Página de login de SI. Accesible sin autenticación (US2).</summary>
[AllowAnonymous]
public class LoginModel : PageModel
{
    /// <summary>Renderiza el formulario de login.</summary>
    public void OnGet()
    {
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SsoAdmin.Web.Pages.Permisos;

/// <summary>Página de gestión de permisos de acceso (US4). Requiere sesión de SI.</summary>
[Authorize]
public class IndexModel : PageModel
{
    /// <summary>Renderiza la página; los datos se cargan por fetch a la API interna.</summary>
    public void OnGet()
    {
    }
}

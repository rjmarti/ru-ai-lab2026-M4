using FluentValidation;

namespace SsoAdmin.Application.Features.AuthSI;

/// <summary>Credenciales de login de un usuario de Seguridad Informática.</summary>
/// <param name="Usuario">Nombre de usuario de SI. Requerido.</param>
/// <param name="Password">Contraseña en claro a validar contra el hash. Requerido.</param>
public sealed record LoginRequest(string Usuario, string Password);

/// <summary>Resultado de un login exitoso.</summary>
/// <param name="Usuario">Nombre de usuario autenticado, usado para el claim de la cookie.</param>
public sealed record LoginResponse(string Usuario);

/// <summary>Valida que el login incluya usuario y contraseña.</summary>
public sealed class LoginValidator : AbstractValidator<LoginRequest>
{
    /// <summary>Configura las reglas de validación.</summary>
    public LoginValidator()
    {
        RuleFor(r => r.Usuario).NotEmpty();
        RuleFor(r => r.Password).NotEmpty();
    }
}

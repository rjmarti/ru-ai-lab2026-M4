using Microsoft.AspNetCore.Identity;
using SsoAdmin.Application.Common;
using SsoAdmin.Data.Repositories;
using SsoAdmin.Models;

namespace SsoAdmin.Application.Features.AuthSI;

/// <summary>
/// Valida las credenciales de un usuario de SI contra <see cref="LoginSI"/> usando
/// <see cref="IPasswordHasher{TUser}"/> (FR-007). No emite la cookie: eso lo hace el
/// controller del host Web tras un resultado exitoso.
/// </summary>
public sealed class LoginHandler
{
    private readonly ILoginSIRepository _repository;
    private readonly IPasswordHasher<LoginSI> _passwordHasher;

    /// <summary>Crea el handler con las dependencias inyectadas.</summary>
    public LoginHandler(ILoginSIRepository repository, IPasswordHasher<LoginSI> passwordHasher)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
    }

    /// <summary>Valida las credenciales; devuelve el usuario autenticado o un error 401.</summary>
    public async Task<Result<LoginResponse>> HandleAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        LoginSI? cuenta = await _repository.ObtenerPorUsuarioAsync(request.Usuario, cancellationToken);
        DomainError credencialesInvalidas = new(ErrorKind.Unauthorized, "Usuario o contraseña inválidos.");

        if (cuenta is null)
        {
            return Result<LoginResponse>.Failure(credencialesInvalidas);
        }

        PasswordVerificationResult verificacion =
            _passwordHasher.VerifyHashedPassword(cuenta, cuenta.PasswordHash, request.Password);

        if (verificacion == PasswordVerificationResult.Failed)
        {
            return Result<LoginResponse>.Failure(credencialesInvalidas);
        }

        return Result<LoginResponse>.Success(new LoginResponse(cuenta.Usuario));
    }
}

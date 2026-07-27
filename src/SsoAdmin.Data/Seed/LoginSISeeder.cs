using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SsoAdmin.Data.Repositories;
using SsoAdmin.Models;

namespace SsoAdmin.Data.Seed;

/// <summary>
/// Precarga, en el primer arranque, una cuenta de SI <c>admin</c>/<c>admin</c> con la
/// contraseña almacenada como hash no reversible vía <see cref="PasswordHasher{TUser}"/>
/// (FR-007). Es idempotente: no hace nada si ya existe al menos una cuenta de SI.
/// </summary>
public class LoginSISeeder
{
    private readonly ILoginSIRepository _repository;
    private readonly IPasswordHasher<LoginSI> _passwordHasher;
    private readonly ILogger<LoginSISeeder> _logger;

    /// <summary>Crea el seeder con las dependencias inyectadas.</summary>
    public LoginSISeeder(
        ILoginSIRepository repository,
        IPasswordHasher<LoginSI> passwordHasher,
        ILogger<LoginSISeeder> logger)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    /// <summary>Precarga la cuenta <c>admin</c> si aún no existe ninguna cuenta de SI.</summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _repository.ExisteAlgunaAsync(cancellationToken))
        {
            return;
        }

        LoginSI admin = new() { Id = Guid.NewGuid(), Usuario = "admin" };
        admin.PasswordHash = _passwordHasher.HashPassword(admin, "admin");
        await _repository.AgregarAsync(admin, cancellationToken);

        _logger.LogInformation("Cuenta de SI 'admin' precargada en el primer arranque (FR-007).");
    }
}

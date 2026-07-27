namespace SsoAdmin.Application.Common;

/// <summary>
/// Clasifica un <see cref="DomainError"/> para que la capa de presentación (controllers)
/// lo traduzca al código HTTP correspondiente sin acoplarse a detalles de negocio.
/// </summary>
public enum ErrorKind
{
    /// <summary>Entrada inválida o regla de negocio de validación → HTTP 400.</summary>
    Validation,

    /// <summary>Recurso no encontrado → HTTP 404.</summary>
    NotFound,

    /// <summary>Conflicto con el estado actual (p. ej. solapamiento) → HTTP 409.</summary>
    Conflict,

    /// <summary>Credenciales ausentes o inválidas → HTTP 401.</summary>
    Unauthorized
}

/// <summary>Error de dominio con su clasificación y un mensaje descriptivo.</summary>
/// <param name="Kind">Categoría del error, usada para mapear el código HTTP.</param>
/// <param name="Message">Mensaje legible para el consumidor de la API.</param>
public sealed record DomainError(ErrorKind Kind, string Message);

/// <summary>
/// Resultado de un caso de uso: éxito con un valor de tipo <typeparamref name="T"/>,
/// o falla con un <see cref="DomainError"/>. Evita usar excepciones para el flujo de negocio.
/// </summary>
/// <typeparam name="T">Tipo del valor devuelto en caso de éxito.</typeparam>
public sealed class Result<T>
{
    private Result(bool isSuccess, T? value, DomainError? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    /// <summary>Indica si el caso de uso finalizó correctamente.</summary>
    public bool IsSuccess { get; }

    /// <summary>Valor devuelto cuando <see cref="IsSuccess"/> es <c>true</c>.</summary>
    public T? Value { get; }

    /// <summary>Error devuelto cuando <see cref="IsSuccess"/> es <c>false</c>.</summary>
    public DomainError? Error { get; }

    /// <summary>Crea un resultado exitoso con el valor indicado.</summary>
    public static Result<T> Success(T value) => new(true, value, null);

    /// <summary>Crea un resultado fallido con el error indicado.</summary>
    public static Result<T> Failure(DomainError error) => new(false, default, error);

    /// <summary>Crea una falla de validación (HTTP 400) con el mensaje indicado.</summary>
    public static Result<T> Validation(string message) => Failure(new DomainError(ErrorKind.Validation, message));

    /// <summary>Crea una falla de no encontrado (HTTP 404) con el mensaje indicado.</summary>
    public static Result<T> NotFound(string message) => Failure(new DomainError(ErrorKind.NotFound, message));

    /// <summary>Crea una falla de conflicto (HTTP 409) con el mensaje indicado.</summary>
    public static Result<T> Conflict(string message) => Failure(new DomainError(ErrorKind.Conflict, message));
}

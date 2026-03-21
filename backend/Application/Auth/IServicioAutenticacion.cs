using Backend.Contracts.Auth;

namespace Backend.Application.Auth;

public interface IServicioAutenticacion
{
    Task<ResultadoLogin> IniciarSesionAsync(
        LoginRequestDto request,
        string? direccionIp,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<(bool Exitoso, int CodigoEstado, RefreshTokenResponseDto? Respuesta, string Codigo, string Mensaje)> RenovarTokenAsync(
        string refreshToken,
        string? direccionIp,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<(bool Exitoso, int CodigoEstado, string Codigo, string Mensaje)> CerrarSesionAsync(
        Guid usuarioId,
        string tokenJti,
        CancellationToken cancellationToken = default);
}

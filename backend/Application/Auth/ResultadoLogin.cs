using Backend.Contracts.Auth;
using Backend.Contracts.Common;

namespace Backend.Application.Auth;

public sealed class ResultadoLogin
{
    public bool Exitoso { get; private init; }
    public int CodigoEstadoHttp { get; private init; }
    public LoginResponseDto? Respuesta { get; private init; }
    public ErrorResponseDto? Error { get; private init; }

    public static ResultadoLogin Ok(LoginResponseDto respuesta) =>
        new()
        {
            Exitoso = true,
            CodigoEstadoHttp = StatusCodes.Status200OK,
            Respuesta = respuesta
        };

    public static ResultadoLogin Fallido(int codigoEstadoHttp, string codigo, string mensaje) =>
        new()
        {
            Exitoso = false,
            CodigoEstadoHttp = codigoEstadoHttp,
            Error = new ErrorResponseDto
            {
                Codigo = codigo,
                Mensaje = mensaje
            }
        };
}

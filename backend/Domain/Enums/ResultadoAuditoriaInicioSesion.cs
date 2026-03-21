namespace Backend.Domain.Enums;

public enum ResultadoAuditoriaInicioSesion
{
    Ok = 1,
    PasswordInvalida = 2,
    UsuarioNoExiste = 3,
    BloqueadoTemporal = 4,
    BloqueadoSistema = 5,
    TokenExpirado = 6,
    UsuarioInactivo = 7
}

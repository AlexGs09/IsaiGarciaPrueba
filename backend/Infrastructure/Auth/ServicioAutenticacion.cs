using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Backend.Application.Auth;
using Backend.Contracts.Auth;
using Backend.Domain.Enums;
using Backend.Infrastructure.Configuration;
using Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Infrastructure.Auth;

public sealed class ServicioAutenticacion : IServicioAutenticacion
{
    private readonly IFabricaConexionSql _fabricaConexionSql;
    private readonly ConfiguracionAutenticacion _configuracionAutenticacion;

    public ServicioAutenticacion(
        IFabricaConexionSql fabricaConexionSql,
        IOptions<ConfiguracionAutenticacion> configuracionAutenticacion)
    {
        _fabricaConexionSql = fabricaConexionSql;
        _configuracionAutenticacion = configuracionAutenticacion.Value;
    }

    public async Task<ResultadoLogin> IniciarSesionAsync(
        LoginRequestDto request,
        string? direccionIp,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var identificadorNormalizado = request.Identificador.Trim();
        var tipoIdentificador = DetectarTipoIdentificador(identificadorNormalizado);
        var ahoraUtc = DateTime.UtcNow;

        await using var connection = _fabricaConexionSql.CrearConexion();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var usuario = await ObtenerUsuarioPorIdentificadorAsync(
            connection,
            (SqlTransaction)transaction,
            tipoIdentificador,
            identificadorNormalizado,
            cancellationToken);

        if (usuario is null)
        {
            await RegistrarAuditoriaAsync(
                connection,
                (SqlTransaction)transaction,
                null,
                identificadorNormalizado,
                tipoIdentificador,
                ResultadoAuditoriaInicioSesion.UsuarioNoExiste,
                direccionIp,
                userAgent,
                ahoraUtc,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return ResultadoLogin.Fallido(
                StatusCodes.Status401Unauthorized,
                "credenciales_invalidas",
                "El usuario y/o la contrasena son incorrectos.");
        }

        if (usuario.Estado == EstadoUsuario.BloqueadoSistema)
        {
            await RegistrarAuditoriaAsync(
                connection,
                (SqlTransaction)transaction,
                usuario.Id,
                identificadorNormalizado,
                tipoIdentificador,
                ResultadoAuditoriaInicioSesion.BloqueadoSistema,
                direccionIp,
                userAgent,
                ahoraUtc,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return ResultadoLogin.Fallido(
                StatusCodes.Status423Locked,
                "usuario_bloqueado_sistema",
                "Tu cuenta ha sido bloqueada por el sistema. Comunicate con soporte.");
        }

        if (usuario.Estado == EstadoUsuario.Inactivo)
        {
            await RegistrarAuditoriaAsync(
                connection,
                (SqlTransaction)transaction,
                usuario.Id,
                identificadorNormalizado,
                tipoIdentificador,
                ResultadoAuditoriaInicioSesion.UsuarioInactivo,
                direccionIp,
                userAgent,
                ahoraUtc,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return ResultadoLogin.Fallido(
                StatusCodes.Status403Forbidden,
                "usuario_inactivo",
                "Tu cuenta se encuentra inactiva. Comunicate con soporte.");
        }

        if (usuario.BloqueadoHasta.HasValue && usuario.BloqueadoHasta.Value > ahoraUtc)
        {
            await RegistrarAuditoriaAsync(
                connection,
                (SqlTransaction)transaction,
                usuario.Id,
                identificadorNormalizado,
                tipoIdentificador,
                ResultadoAuditoriaInicioSesion.BloqueadoTemporal,
                direccionIp,
                userAgent,
                ahoraUtc,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return ResultadoLogin.Fallido(
                StatusCodes.Status423Locked,
                "usuario_bloqueado_temporal",
                "Tu cuenta esta bloqueada temporalmente. Intenta nuevamente en 15 minutos.");
        }

        var passwordValida = VerificarContrasena(request.Contrasena, usuario.ContrasenaHash);

        if (!passwordValida)
        {
            usuario.IntentosFallidos += 1;
            ResultadoAuditoriaInicioSesion resultadoAuditoria = ResultadoAuditoriaInicioSesion.PasswordInvalida;

            if (usuario.IntentosFallidos >= _configuracionAutenticacion.MaximoIntentosFallidosLogin)
            {
                usuario.BloqueadoHasta = ahoraUtc.AddMinutes(_configuracionAutenticacion.MinutosBloqueoPorIntentos);
                resultadoAuditoria = ResultadoAuditoriaInicioSesion.BloqueadoTemporal;
            }

            usuario.UpdatedAt = ahoraUtc;

            await ActualizarEstadoFallidoAsync(
                connection,
                (SqlTransaction)transaction,
                usuario,
                cancellationToken);

            await RegistrarAuditoriaAsync(
                connection,
                (SqlTransaction)transaction,
                usuario.Id,
                identificadorNormalizado,
                tipoIdentificador,
                resultadoAuditoria,
                direccionIp,
                userAgent,
                ahoraUtc,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var mensaje = resultadoAuditoria == ResultadoAuditoriaInicioSesion.BloqueadoTemporal
                ? "Tu cuenta ha sido bloqueada temporalmente por 15 minutos debido a multiples intentos fallidos."
                : "El usuario y/o la contrasena son incorrectos.";

            var codigo = resultadoAuditoria == ResultadoAuditoriaInicioSesion.BloqueadoTemporal
                ? "usuario_bloqueado_temporal"
                : "credenciales_invalidas";

            var estadoHttp = resultadoAuditoria == ResultadoAuditoriaInicioSesion.BloqueadoTemporal
                ? StatusCodes.Status423Locked
                : StatusCodes.Status401Unauthorized;

            return ResultadoLogin.Fallido(estadoHttp, codigo, mensaje);
        }

        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        usuario.MotivoBloqueo = null;
        usuario.UltimoLoginAt = ahoraUtc;
        usuario.UpdatedAt = ahoraUtc;

        var tokenJti = Guid.NewGuid().ToString("N");
        var accessToken = GenerarAccessToken(usuario, tokenJti, ahoraUtc);
        var refreshTokenPlano = GenerarRefreshToken();
        var refreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshTokenPlano);
        var expiraEn = ahoraUtc.AddMinutes(_configuracionAutenticacion.MinutosDuracionTokenAcceso);
        var refreshTokenExpiraEn = ahoraUtc.AddDays(_configuracionAutenticacion.DiasDuracionRefreshToken);

        await ActualizarEstadoExitosoAsync(
            connection,
            (SqlTransaction)transaction,
            usuario,
            cancellationToken);

        await CrearSesionAsync(
            connection,
            (SqlTransaction)transaction,
            usuario.Id,
            tokenJti,
            refreshTokenHash,
            direccionIp,
            userAgent,
            ahoraUtc,
            expiraEn,
            refreshTokenExpiraEn,
            cancellationToken);

        await RegistrarAuditoriaAsync(
            connection,
            (SqlTransaction)transaction,
            usuario.Id,
            identificadorNormalizado,
            tipoIdentificador,
            ResultadoAuditoriaInicioSesion.Ok,
            direccionIp,
            userAgent,
            ahoraUtc,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return ResultadoLogin.Ok(new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenPlano,
            ExpiresInSeconds = _configuracionAutenticacion.MinutosDuracionTokenAcceso * 60,
            Usuario = new UsuarioSesionDto
            {
                Id = usuario.Id,
                Dni = usuario.Dni,
                Username = usuario.Username,
                Nombres = usuario.Nombres,
                PrimerApellido = usuario.PrimerApellido,
                SegundoApellido = usuario.SegundoApellido,
                Correo = usuario.Correo
            }
        });
    }

    public async Task<(bool Exitoso, int CodigoEstado, RefreshTokenResponseDto? Respuesta, string Codigo, string Mensaje)> RenovarTokenAsync(
        string refreshToken,
        string? direccionIp,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var ahoraUtc = DateTime.UtcNow;

        await using var connection = _fabricaConexionSql.CrearConexion();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var sesion = await ObtenerSesionPorRefreshTokenAsync(connection, (SqlTransaction)transaction, refreshToken, ahoraUtc, cancellationToken);
        if (sesion is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return (false, StatusCodes.Status401Unauthorized, null, "refresh_token_invalido", "El refresh token no es valido o ha expirado.");
        }

        if (sesion.EstadoUsuario == EstadoUsuario.BloqueadoSistema)
        {
            await transaction.CommitAsync(cancellationToken);
            return (false, StatusCodes.Status423Locked, null, "usuario_bloqueado_sistema", "Tu cuenta ha sido bloqueada por el sistema. Comunicate con soporte.");
        }

        if (sesion.EstadoUsuario == EstadoUsuario.Inactivo)
        {
            await transaction.CommitAsync(cancellationToken);
            return (false, StatusCodes.Status403Forbidden, null, "usuario_inactivo", "Tu cuenta se encuentra inactiva. Comunicate con soporte.");
        }

        var nuevoTokenJti = Guid.NewGuid().ToString("N");
        var nuevoAccessToken = GenerarAccessToken(sesion, nuevoTokenJti, ahoraUtc);
        var nuevoRefreshTokenPlano = GenerarRefreshToken();
        var nuevoRefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(nuevoRefreshTokenPlano);
        var nuevaExpiracionAccessToken = ahoraUtc.AddMinutes(_configuracionAutenticacion.MinutosDuracionTokenAcceso);
        var nuevaExpiracionRefreshToken = ahoraUtc.AddDays(_configuracionAutenticacion.DiasDuracionRefreshToken);

        await ActualizarSesionPorRefreshAsync(
            connection,
            (SqlTransaction)transaction,
            sesion.Id,
            nuevoTokenJti,
            nuevoRefreshTokenHash,
            direccionIp,
            userAgent,
            nuevaExpiracionAccessToken,
            nuevaExpiracionRefreshToken,
            ahoraUtc,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return (
            true,
            StatusCodes.Status200OK,
            new RefreshTokenResponseDto
            {
                AccessToken = nuevoAccessToken,
                RefreshToken = nuevoRefreshTokenPlano,
                ExpiresInSeconds = _configuracionAutenticacion.MinutosDuracionTokenAcceso * 60
            },
            string.Empty,
            string.Empty);
    }

    public async Task<(bool Exitoso, int CodigoEstado, string Codigo, string Mensaje)> CerrarSesionAsync(
        Guid usuarioId,
        string tokenJti,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.SesionesUsuario
            SET
                CerradaEn = @cerradaEn,
                MotivoCierre = 'LOGOUT'
            WHERE UsuarioId = @usuarioId
              AND TokenJti = @tokenJti
              AND CerradaEn IS NULL
            """;

        await using var connection = _fabricaConexionSql.CrearConexion();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@cerradaEn", DateTime.UtcNow);
        command.Parameters.AddWithValue("@usuarioId", usuarioId);
        command.Parameters.AddWithValue("@tokenJti", tokenJti);

        var filas = await command.ExecuteNonQueryAsync(cancellationToken);
        if (filas == 0)
        {
            return (false, StatusCodes.Status404NotFound, "sesion_no_encontrada", "No se encontro una sesion activa para cerrar.");
        }

        return (true, StatusCodes.Status200OK, "logout_ok", "Sesion cerrada correctamente.");
    }

    private async Task<UsuarioAutenticacionRecord?> ObtenerUsuarioPorIdentificadorAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        TipoIdentificadorIngreso tipoIdentificador,
        string identificador,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (1)
                Id,
                Dni,
                Username,
                Correo,
                ContrasenaHash,
                Nombres,
                PrimerApellido,
                SegundoApellido,
                Estado,
                BloqueadoHasta,
                MotivoBloqueo,
                IntentosFallidos,
                UltimoLoginAt,
                UpdatedAt
            FROM dbo.Usuarios
            WHERE
                (@tipo = 'CORREO' AND LOWER(Correo) = LOWER(@identificador))
                OR (@tipo = 'USERNAME' AND Username = @identificador)
                OR (@tipo = 'DNI' AND Dni = @identificador)
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@tipo", TipoIdentificadorToDb(tipoIdentificador));
        command.Parameters.AddWithValue("@identificador", identificador);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new UsuarioAutenticacionRecord
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            Dni = reader.GetString(reader.GetOrdinal("Dni")),
            Username = reader.GetString(reader.GetOrdinal("Username")),
            Correo = reader.GetString(reader.GetOrdinal("Correo")),
            ContrasenaHash = reader.GetString(reader.GetOrdinal("ContrasenaHash")),
            Nombres = reader.GetString(reader.GetOrdinal("Nombres")),
            PrimerApellido = reader.GetString(reader.GetOrdinal("PrimerApellido")),
            SegundoApellido = reader.IsDBNull(reader.GetOrdinal("SegundoApellido"))
                ? null
                : reader.GetString(reader.GetOrdinal("SegundoApellido")),
            Estado = DbToEstadoUsuario(reader.GetString(reader.GetOrdinal("Estado"))),
            BloqueadoHasta = reader.IsDBNull(reader.GetOrdinal("BloqueadoHasta"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("BloqueadoHasta")),
            MotivoBloqueo = reader.IsDBNull(reader.GetOrdinal("MotivoBloqueo"))
                ? null
                : reader.GetString(reader.GetOrdinal("MotivoBloqueo")),
            IntentosFallidos = reader.GetInt32(reader.GetOrdinal("IntentosFallidos")),
            UltimoLoginAt = reader.IsDBNull(reader.GetOrdinal("UltimoLoginAt"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("UltimoLoginAt")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
        };
    }

    private static bool VerificarContrasena(string contrasenaPlano, string contrasenaHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(contrasenaPlano, contrasenaHash);
        }
        catch
        {
            return false;
        }
    }

    private async Task ActualizarEstadoFallidoAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        UsuarioAutenticacionRecord usuario,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.Usuarios
            SET
                IntentosFallidos = @intentosFallidos,
                BloqueadoHasta = @bloqueadoHasta,
                UpdatedAt = @updatedAt
            WHERE Id = @id
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@id", usuario.Id);
        command.Parameters.AddWithValue("@intentosFallidos", usuario.IntentosFallidos);
        command.Parameters.AddWithValue("@bloqueadoHasta", (object?)usuario.BloqueadoHasta ?? DBNull.Value);
        command.Parameters.AddWithValue("@updatedAt", usuario.UpdatedAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task ActualizarEstadoExitosoAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        UsuarioAutenticacionRecord usuario,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.Usuarios
            SET
                IntentosFallidos = 0,
                BloqueadoHasta = NULL,
                MotivoBloqueo = NULL,
                UltimoLoginAt = @ultimoLoginAt,
                UpdatedAt = @updatedAt
            WHERE Id = @id
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@id", usuario.Id);
        command.Parameters.AddWithValue("@ultimoLoginAt", (object?)usuario.UltimoLoginAt ?? DBNull.Value);
        command.Parameters.AddWithValue("@updatedAt", usuario.UpdatedAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task CrearSesionAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        Guid usuarioId,
        string tokenJti,
        string refreshTokenHash,
        string? direccionIp,
        string? userAgent,
        DateTime iniciaEn,
        DateTime expiraEn,
        DateTime refreshTokenExpiraEn,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.SesionesUsuario
            (
                Id,
                UsuarioId,
                TokenJti,
                RefreshTokenHash,
                Ip,
                UserAgent,
                IniciaEn,
                ExpiraEn,
                RefreshTokenExpiraEn,
                UltimoMovimientoEn
            )
            VALUES
            (
                @id,
                @usuarioId,
                @tokenJti,
                @refreshTokenHash,
                @ip,
                @userAgent,
                @iniciaEn,
                @expiraEn,
                @refreshTokenExpiraEn,
                @ultimoMovimientoEn
            )
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@id", Guid.NewGuid());
        command.Parameters.AddWithValue("@usuarioId", usuarioId);
        command.Parameters.AddWithValue("@tokenJti", tokenJti);
        command.Parameters.AddWithValue("@refreshTokenHash", refreshTokenHash);
        command.Parameters.AddWithValue("@ip", (object?)direccionIp ?? DBNull.Value);
        command.Parameters.AddWithValue("@userAgent", (object?)userAgent ?? DBNull.Value);
        command.Parameters.AddWithValue("@iniciaEn", iniciaEn);
        command.Parameters.AddWithValue("@expiraEn", expiraEn);
        command.Parameters.AddWithValue("@refreshTokenExpiraEn", refreshTokenExpiraEn);
        command.Parameters.AddWithValue("@ultimoMovimientoEn", iniciaEn);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task ActualizarSesionPorRefreshAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        Guid sesionId,
        string nuevoTokenJti,
        string nuevoRefreshTokenHash,
        string? direccionIp,
        string? userAgent,
        DateTime nuevaExpiracionAccessToken,
        DateTime nuevaExpiracionRefreshToken,
        DateTime ultimoMovimientoEn,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.SesionesUsuario
            SET
                TokenJti = @nuevoTokenJti,
                RefreshTokenHash = @nuevoRefreshTokenHash,
                Ip = @ip,
                UserAgent = @userAgent,
                ExpiraEn = @expiraEn,
                RefreshTokenExpiraEn = @refreshTokenExpiraEn,
                UltimoMovimientoEn = @ultimoMovimientoEn
            WHERE Id = @sesionId
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@sesionId", sesionId);
        command.Parameters.AddWithValue("@nuevoTokenJti", nuevoTokenJti);
        command.Parameters.AddWithValue("@nuevoRefreshTokenHash", nuevoRefreshTokenHash);
        command.Parameters.AddWithValue("@ip", (object?)direccionIp ?? DBNull.Value);
        command.Parameters.AddWithValue("@userAgent", (object?)userAgent ?? DBNull.Value);
        command.Parameters.AddWithValue("@expiraEn", nuevaExpiracionAccessToken);
        command.Parameters.AddWithValue("@refreshTokenExpiraEn", nuevaExpiracionRefreshToken);
        command.Parameters.AddWithValue("@ultimoMovimientoEn", ultimoMovimientoEn);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task RegistrarAuditoriaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        Guid? usuarioId,
        string identificadorIngresado,
        TipoIdentificadorIngreso tipoIdentificador,
        ResultadoAuditoriaInicioSesion resultado,
        string? direccionIp,
        string? userAgent,
        DateTime fechaEvento,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.AuditoriaInicioSesion
            (
                UsuarioId,
                IdentificadorIngresado,
                TipoIdentificador,
                Resultado,
                Ip,
                UserAgent,
                FechaEvento
            )
            VALUES
            (
                @usuarioId,
                @identificadorIngresado,
                @tipoIdentificador,
                @resultado,
                @ip,
                @userAgent,
                @fechaEvento
            )
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@usuarioId", (object?)usuarioId ?? DBNull.Value);
        command.Parameters.AddWithValue("@identificadorIngresado", identificadorIngresado);
        command.Parameters.AddWithValue("@tipoIdentificador", TipoIdentificadorToDb(tipoIdentificador));
        command.Parameters.AddWithValue("@resultado", ResultadoAuditoriaToDb(resultado));
        command.Parameters.AddWithValue("@ip", (object?)direccionIp ?? DBNull.Value);
        command.Parameters.AddWithValue("@userAgent", (object?)userAgent ?? DBNull.Value);
        command.Parameters.AddWithValue("@fechaEvento", fechaEvento);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private string GenerarAccessToken(UsuarioAutenticacionRecord usuario, string tokenJti, DateTime ahoraUtc)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, tokenJti),
            new(JwtRegisteredClaimNames.Email, usuario.Correo),
            new("dni", usuario.Dni),
            new("username", usuario.Username),
            new("nombres", usuario.Nombres),
            new("primer_apellido", usuario.PrimerApellido)
        };

        if (!string.IsNullOrWhiteSpace(usuario.SegundoApellido))
        {
            claims.Add(new Claim("segundo_apellido", usuario.SegundoApellido));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuracionAutenticacion.LlaveSecretaJwt));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiraEn = ahoraUtc.AddMinutes(_configuracionAutenticacion.MinutosDuracionTokenAcceso);

        var token = new JwtSecurityToken(
            issuer: _configuracionAutenticacion.EmisorJwt,
            audience: _configuracionAutenticacion.AudienciaJwt,
            claims: claims,
            notBefore: ahoraUtc,
            expires: expiraEn,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerarAccessToken(SesionRefreshRecord usuario, string tokenJti, DateTime ahoraUtc)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.UsuarioId.ToString()),
            new(JwtRegisteredClaimNames.Jti, tokenJti),
            new(JwtRegisteredClaimNames.Email, usuario.Correo),
            new("dni", usuario.Dni),
            new("username", usuario.Username),
            new("nombres", usuario.Nombres),
            new("primer_apellido", usuario.PrimerApellido)
        };

        if (!string.IsNullOrWhiteSpace(usuario.SegundoApellido))
        {
            claims.Add(new Claim("segundo_apellido", usuario.SegundoApellido));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuracionAutenticacion.LlaveSecretaJwt));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiraEn = ahoraUtc.AddMinutes(_configuracionAutenticacion.MinutosDuracionTokenAcceso);

        var token = new JwtSecurityToken(
            issuer: _configuracionAutenticacion.EmisorJwt,
            audience: _configuracionAutenticacion.AudienciaJwt,
            claims: claims,
            notBefore: ahoraUtc,
            expires: expiraEn,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<SesionRefreshRecord?> ObtenerSesionPorRefreshTokenAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string refreshToken,
        DateTime ahoraUtc,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                s.Id,
                s.UsuarioId,
                s.TokenJti,
                s.RefreshTokenHash,
                s.RefreshTokenExpiraEn,
                u.Dni,
                u.Username,
                u.Correo,
                u.Nombres,
                u.PrimerApellido,
                u.SegundoApellido,
                u.Estado
            FROM dbo.SesionesUsuario s
            INNER JOIN dbo.Usuarios u ON u.Id = s.UsuarioId
            WHERE s.CerradaEn IS NULL
              AND s.RefreshTokenExpiraEn >= @ahoraUtc
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@ahoraUtc", ahoraUtc);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var sesiones = new List<SesionRefreshRecord>();

        while (await reader.ReadAsync(cancellationToken))
        {
            sesiones.Add(new SesionRefreshRecord
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                UsuarioId = reader.GetGuid(reader.GetOrdinal("UsuarioId")),
                TokenJti = reader.GetString(reader.GetOrdinal("TokenJti")),
                RefreshTokenHash = reader.GetString(reader.GetOrdinal("RefreshTokenHash")),
                RefreshTokenExpiraEn = reader.GetDateTime(reader.GetOrdinal("RefreshTokenExpiraEn")),
                Dni = reader.GetString(reader.GetOrdinal("Dni")),
                Username = reader.GetString(reader.GetOrdinal("Username")),
                Correo = reader.GetString(reader.GetOrdinal("Correo")),
                Nombres = reader.GetString(reader.GetOrdinal("Nombres")),
                PrimerApellido = reader.GetString(reader.GetOrdinal("PrimerApellido")),
                SegundoApellido = reader.IsDBNull(reader.GetOrdinal("SegundoApellido"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("SegundoApellido")),
                EstadoUsuario = DbToEstadoUsuario(reader.GetString(reader.GetOrdinal("Estado")))
            });
        }

        return sesiones.FirstOrDefault(sesion => BCrypt.Net.BCrypt.Verify(refreshToken, sesion.RefreshTokenHash));
    }

    private string GenerarRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static TipoIdentificadorIngreso DetectarTipoIdentificador(string identificador)
    {
        if (identificador.Contains('@'))
        {
            return TipoIdentificadorIngreso.Correo;
        }

        if (identificador.All(char.IsDigit))
        {
            return TipoIdentificadorIngreso.Dni;
        }

        return string.IsNullOrWhiteSpace(identificador)
            ? TipoIdentificadorIngreso.Desconocido
            : TipoIdentificadorIngreso.Username;
    }

    private static EstadoUsuario DbToEstadoUsuario(string estado)
    {
        return estado switch
        {
            "ACTIVO" => EstadoUsuario.Activo,
            "BLOQUEADO_SISTEMA" => EstadoUsuario.BloqueadoSistema,
            "INACTIVO" => EstadoUsuario.Inactivo,
            _ => EstadoUsuario.Inactivo
        };
    }

    private static string TipoIdentificadorToDb(TipoIdentificadorIngreso tipoIdentificador)
    {
        return tipoIdentificador switch
        {
            TipoIdentificadorIngreso.Correo => "CORREO",
            TipoIdentificadorIngreso.Username => "USERNAME",
            TipoIdentificadorIngreso.Dni => "DNI",
            _ => "DESCONOCIDO"
        };
    }

    private static string ResultadoAuditoriaToDb(ResultadoAuditoriaInicioSesion resultado)
    {
        return resultado switch
        {
            ResultadoAuditoriaInicioSesion.Ok => "OK",
            ResultadoAuditoriaInicioSesion.PasswordInvalida => "PASSWORD_INVALIDA",
            ResultadoAuditoriaInicioSesion.UsuarioNoExiste => "USUARIO_NO_EXISTE",
            ResultadoAuditoriaInicioSesion.BloqueadoTemporal => "BLOQUEADO_TEMPORAL",
            ResultadoAuditoriaInicioSesion.BloqueadoSistema => "BLOQUEADO_SISTEMA",
            ResultadoAuditoriaInicioSesion.TokenExpirado => "TOKEN_EXPIRADO",
            ResultadoAuditoriaInicioSesion.UsuarioInactivo => "USUARIO_INACTIVO",
            _ => "USUARIO_NO_EXISTE"
        };
    }

    private sealed class UsuarioAutenticacionRecord
    {
        public Guid Id { get; set; }
        public string Dni { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string ContrasenaHash { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string PrimerApellido { get; set; } = string.Empty;
        public string? SegundoApellido { get; set; }
        public EstadoUsuario Estado { get; set; }
        public DateTime? BloqueadoHasta { get; set; }
        public string? MotivoBloqueo { get; set; }
        public int IntentosFallidos { get; set; }
        public DateTime? UltimoLoginAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    private sealed class SesionRefreshRecord
    {
        public Guid Id { get; set; }
        public Guid UsuarioId { get; set; }
        public string TokenJti { get; set; } = string.Empty;
        public string RefreshTokenHash { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiraEn { get; set; }
        public string Dni { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string PrimerApellido { get; set; } = string.Empty;
        public string? SegundoApellido { get; set; }
        public EstadoUsuario EstadoUsuario { get; set; }
    }
}

using Backend.Application.Usuarios;
using Backend.Contracts.Usuarios;
using Backend.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace Backend.Infrastructure.Usuarios;

public sealed class ServicioUsuarios : IServicioUsuarios
{
    private readonly IFabricaConexionSql _fabricaConexionSql;

    public ServicioUsuarios(IFabricaConexionSql fabricaConexionSql)
    {
        _fabricaConexionSql = fabricaConexionSql;
    }

    public async Task<PerfilUsuarioResponseDto?> ObtenerPerfilAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP (1)
                Dni,
                Nombres,
                PrimerApellido,
                SegundoApellido,
                FechaNacimiento,
                Nacionalidad,
                Correo,
                NumeroCelular
            FROM dbo.Usuarios
            WHERE Id = @usuarioId
            """;

        await using var connection = _fabricaConexionSql.CrearConexion();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@usuarioId", usuarioId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new PerfilUsuarioResponseDto
        {
            Dni = reader.GetString(reader.GetOrdinal("Dni")),
            Nombres = reader.GetString(reader.GetOrdinal("Nombres")),
            PrimerApellido = reader.GetString(reader.GetOrdinal("PrimerApellido")),
            SegundoApellido = reader.IsDBNull(reader.GetOrdinal("SegundoApellido"))
                ? null
                : reader.GetString(reader.GetOrdinal("SegundoApellido")),
            FechaNacimiento = DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("FechaNacimiento"))),
            Nacionalidad = reader.GetString(reader.GetOrdinal("Nacionalidad")),
            Correo = reader.GetString(reader.GetOrdinal("Correo")),
            NumeroCelular = reader.GetString(reader.GetOrdinal("NumeroCelular"))
        };
    }
}

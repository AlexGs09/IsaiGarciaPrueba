using Microsoft.Data.SqlClient;

namespace Backend.Infrastructure.Data;

public sealed class FabricaConexionSql : IFabricaConexionSql
{
    private readonly string _connectionString;

    public FabricaConexionSql(IConfiguration configuration)
    {
        _connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? string.Empty;
    }

    public SqlConnection CrearConexion()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("No existe una cadena de conexion configurada para SQL Server.");
        }

        return new SqlConnection(_connectionString);
    }
}

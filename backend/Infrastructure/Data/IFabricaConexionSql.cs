using Microsoft.Data.SqlClient;

namespace Backend.Infrastructure.Data;

public interface IFabricaConexionSql
{
    SqlConnection CrearConexion();
}

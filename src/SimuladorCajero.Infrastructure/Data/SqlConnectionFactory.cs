using Microsoft.Data.SqlClient;

namespace SimuladorCajero.Infrastructure.Data;

public sealed class SqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException(
                "La cadena de conexión es obligatoria.",
                nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    public SqlConnection CrearConexion()
    {
        return new SqlConnection(_connectionString);
    }
}
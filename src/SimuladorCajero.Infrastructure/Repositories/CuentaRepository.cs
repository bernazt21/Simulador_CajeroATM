using System.Data;
using Microsoft.Data.SqlClient;
using SimuladorCajero.Application.DTOs;
using SimuladorCajero.Application.Interfaces;
using SimuladorCajero.Infrastructure.Data;

namespace SimuladorCajero.Infrastructure.Repositories;

public sealed class CuentaRepository : ICuentaRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public CuentaRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory
            ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<SaldoDto?> ObtenerSaldoAsync(
        int idCuenta,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            _connectionFactory.CrearConexion();

        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            "dbo.sp_ConsultarSaldo",
            connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(
            new SqlParameter("@IdCuenta", SqlDbType.Int)
            {
                Value = idCuenta
            });

        try
        {
            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new SaldoDto
            {
                IdCuenta = reader.GetInt32(
                    reader.GetOrdinal("IdCuenta")),

                NumeroCuenta = reader.GetString(
                    reader.GetOrdinal("NumeroCuenta")),

                Saldo = reader.GetDecimal(
                    reader.GetOrdinal("Saldo")),

                Activa = reader.GetBoolean(
                    reader.GetOrdinal("Activa"))
            };
        }
        catch (SqlException exception)
            when (exception.Number == 50001)
        {
            // El procedimiento almacenado utiliza este error cuando
            // la cuenta no existe o se encuentra inactiva.
            return null;
        }
    }
}
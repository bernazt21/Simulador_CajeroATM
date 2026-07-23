using System.Data;
using Microsoft.Data.SqlClient;
using SimuladorCajero.Application.DTOs;
using SimuladorCajero.Application.Exceptions;
using SimuladorCajero.Application.Interfaces;
using SimuladorCajero.Infrastructure.Data;

namespace SimuladorCajero.Infrastructure.Repositories;

public sealed class TransaccionRepository : ITransaccionRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public TransaccionRepository(
        SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory
            ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public Task<MovimientoResultadoDto> RegistrarDepositoAsync(
        MovimientoRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return EjecutarMovimientoAsync(
            "dbo.sp_RegistrarDeposito",
            request,
            cancellationToken);
    }

    public Task<MovimientoResultadoDto> RegistrarRetiroAsync(
        MovimientoRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return EjecutarMovimientoAsync(
            "dbo.sp_RegistrarRetiro",
            request,
            cancellationToken);
    }

    public async Task<MovimientoResultadoDto> RevertirAsync(
        long idTransaccion,
        ReversionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection =
            _connectionFactory.CrearConexion();

        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            "dbo.sp_RevertirTransaccion",
            connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(
            new SqlParameter("@IdTransaccion", SqlDbType.BigInt)
            {
                Value = idTransaccion
            });

        command.Parameters.Add(
            new SqlParameter("@Motivo", SqlDbType.NVarChar, 250)
            {
                Value = request.Motivo
            });

        try
        {
            await using var reader =
                await command.ExecuteReaderAsync(
                    CommandBehavior.SingleRow,
                    cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException(
                    "El procedimiento de reversión no devolvió resultados.");
            }

            return new MovimientoResultadoDto
            {
                IdTransaccion = reader.GetInt64(
                    reader.GetOrdinal("IdReversion")),

                Tipo = "REVERSO",

                Monto = reader.GetDecimal(
                    reader.GetOrdinal("Monto")),

                SaldoAnterior = reader.GetDecimal(
                    reader.GetOrdinal("SaldoAnterior")),

                SaldoPosterior = reader.GetDecimal(
                    reader.GetOrdinal("SaldoPosterior")),

                Mensaje = reader.GetString(
                    reader.GetOrdinal("Mensaje")),

                IdTransaccionOriginal = reader.GetInt64(
                    reader.GetOrdinal("IdTransaccionOriginal"))
            };
        }
        catch (SqlException exception)
            when (exception.Number >= 50000)
        {
            throw new ReglaNegocioException(
                exception.Message,
                exception);
        }
    }

    private async Task<MovimientoResultadoDto>
        EjecutarMovimientoAsync(
            string procedimiento,
            MovimientoRequest request,
            CancellationToken cancellationToken)
    {
        await using var connection =
            _connectionFactory.CrearConexion();

        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            procedimiento,
            connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(
            new SqlParameter("@IdCuenta", SqlDbType.Int)
            {
                Value = request.IdCuenta
            });

        var montoParameter =
            command.Parameters.Add("@Monto", SqlDbType.Decimal);

        montoParameter.Precision = 18;
        montoParameter.Scale = 2;
        montoParameter.Value = request.Monto;

        try
        {
            await using var reader =
                await command.ExecuteReaderAsync(
                    CommandBehavior.SingleRow,
                    cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException(
                    "El procedimiento del movimiento no devolvió resultados.");
            }

            return new MovimientoResultadoDto
            {
                IdTransaccion = reader.GetInt64(
                    reader.GetOrdinal("IdTransaccion")),

                Tipo = reader.GetString(
                    reader.GetOrdinal("Tipo")),

                Monto = reader.GetDecimal(
                    reader.GetOrdinal("Monto")),

                SaldoAnterior = reader.GetDecimal(
                    reader.GetOrdinal("SaldoAnterior")),

                SaldoPosterior = reader.GetDecimal(
                    reader.GetOrdinal("SaldoPosterior")),

                Mensaje = reader.GetString(
                    reader.GetOrdinal("Mensaje")),

                IdTransaccionOriginal = null
            };
        }
        catch (SqlException exception)
            when (exception.Number >= 50000)
        {
            throw new ReglaNegocioException(
                exception.Message,
                exception);
        }
    }
}
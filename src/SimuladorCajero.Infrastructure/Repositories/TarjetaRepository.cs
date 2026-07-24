using System.Data;
using Microsoft.Data.SqlClient;
using SimuladorCajero.Application.Interfaces;
using SimuladorCajero.Domain.Entities;
using SimuladorCajero.Infrastructure.Data;

namespace SimuladorCajero.Infrastructure.Repositories;

public sealed class TarjetaRepository : ITarjetaRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public TarjetaRepository(
        SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory
            ?? throw new ArgumentNullException(
                nameof(connectionFactory));
    }

    public async Task<Tarjeta?> ObtenerPorNumeroAsync(
        string numeroTarjeta,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(numeroTarjeta))
        {
            return null;
        }

        const string sql = """
            SELECT
                IdTarjeta,
                IdCuenta,
                NumeroTarjeta,
                NipHash,
                Bloqueada,
                IntentosFallidos,
                FechaExpiracion,
                Activa,
                FechaCreacion
            FROM dbo.Tarjetas
            WHERE NumeroTarjeta = @NumeroTarjeta;
            """;

        await using var connection =
            _connectionFactory.CrearConexion();

        await connection.OpenAsync(cancellationToken);

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.Add(
            new SqlParameter(
                "@NumeroTarjeta",
                SqlDbType.Char,
                16)
            {
                Value = numeroTarjeta
            });

        await using var reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var tarjeta = new Tarjeta
        {
            IdTarjeta = reader.GetInt32(
                reader.GetOrdinal("IdTarjeta")),

            IdCuenta = reader.GetInt32(
                reader.GetOrdinal("IdCuenta")),

            NumeroTarjeta = reader.GetString(
                reader.GetOrdinal("NumeroTarjeta")),

            FechaExpiracion = reader.GetDateTime(
                reader.GetOrdinal("FechaExpiracion")),

            Activa = reader.GetBoolean(
                reader.GetOrdinal("Activa")),

            FechaCreacion = reader.GetDateTime(
                reader.GetOrdinal("FechaCreacion"))
        };

        // Cargar el hash guardado en SQL Server.
        // Sin esta asignación, NipHash quedaba vacío y
        // BCrypt siempre devolvía false.
        tarjeta.EstablecerNipHash(
            reader.GetString(
                reader.GetOrdinal("NipHash")));

        // Cargar los intentos y el estado de bloqueo.
        tarjeta.CargarEstadoSeguridad(
            reader.GetByte(
                reader.GetOrdinal("IntentosFallidos")),
            reader.GetBoolean(
                reader.GetOrdinal("Bloqueada")));

        return tarjeta;
    }

    public async Task CambiarNipAsync(
        int idTarjeta,
        string nuevoNipHash,
        CancellationToken cancellationToken = default)
    {
        if (idTarjeta <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(idTarjeta),
                "El identificador de la tarjeta no es válido.");
        }

        if (string.IsNullOrWhiteSpace(nuevoNipHash))
        {
            throw new ArgumentException(
                "El hash del nuevo NIP es obligatorio.",
                nameof(nuevoNipHash));
        }

        await using var connection =
            _connectionFactory.CrearConexion();

        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            "dbo.sp_CambiarNip",
            connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(
            new SqlParameter(
                "@IdTarjeta",
                SqlDbType.Int)
            {
                Value = idTarjeta
            });

        command.Parameters.Add(
            new SqlParameter(
                "@NuevoNipHash",
                SqlDbType.NVarChar,
                255)
            {
                Value = nuevoNipHash
            });

        await command.ExecuteNonQueryAsync(
            cancellationToken);
    }

    public async Task ActualizarEstadoAsync(
        Tarjeta tarjeta,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tarjeta);

        const string sql = """
            UPDATE dbo.Tarjetas
            SET
                Bloqueada = @Bloqueada,
                IntentosFallidos = @IntentosFallidos,
                Activa = @Activa
            WHERE IdTarjeta = @IdTarjeta;
            """;

        await using var connection =
            _connectionFactory.CrearConexion();

        await connection.OpenAsync(cancellationToken);

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.Add(
            new SqlParameter(
                "@Bloqueada",
                SqlDbType.Bit)
            {
                Value = tarjeta.Bloqueada
            });

        command.Parameters.Add(
            new SqlParameter(
                "@IntentosFallidos",
                SqlDbType.TinyInt)
            {
                Value = tarjeta.IntentosFallidos
            });

        command.Parameters.Add(
            new SqlParameter(
                "@Activa",
                SqlDbType.Bit)
            {
                Value = tarjeta.Activa
            });

        command.Parameters.Add(
            new SqlParameter(
                "@IdTarjeta",
                SqlDbType.Int)
            {
                Value = tarjeta.IdTarjeta
            });

        var filasAfectadas =
            await command.ExecuteNonQueryAsync(
                cancellationToken);

        if (filasAfectadas == 0)
        {
            throw new InvalidOperationException(
                "No fue posible actualizar la tarjeta porque no existe.");
        }
    }
}
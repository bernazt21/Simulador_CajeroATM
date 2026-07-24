using SimuladorCajero.Domain.Entities;

namespace SimuladorCajero.Application.Interfaces;

public interface ITarjetaRepository
{
    Task<Tarjeta?> ObtenerPorNumeroAsync(
        string numeroTarjeta,
        CancellationToken cancellationToken = default);

    Task CambiarNipAsync(
        int idTarjeta,
        string nuevoNipHash,
        CancellationToken cancellationToken = default);

    Task ActualizarEstadoAsync(
        Tarjeta tarjeta,
        CancellationToken cancellationToken = default);
}
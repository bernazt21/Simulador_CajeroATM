using SimuladorCajero.Application.DTOs;

namespace SimuladorCajero.Application.Interfaces;

public interface ICuentaRepository
{
    Task<SaldoDto?> ObtenerSaldoAsync(
        int idCuenta,
        CancellationToken cancellationToken = default);
}
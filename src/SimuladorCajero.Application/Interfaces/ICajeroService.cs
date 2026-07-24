using SimuladorCajero.Application.DTOs;

namespace SimuladorCajero.Application.Interfaces;

public interface ICajeroService
{
    Task<SaldoDto> ConsultarSaldoAsync(
        int idCuenta,
        CancellationToken cancellationToken = default);

    Task<MovimientoResultadoDto> DepositarAsync(
        MovimientoRequest request,
        CancellationToken cancellationToken = default);

    Task<MovimientoResultadoDto> RetirarAsync(
        MovimientoRequest request,
        CancellationToken cancellationToken = default);

    Task CambiarNipAsync(
        CambioNipRequest request,
        CancellationToken cancellationToken = default);

    Task<MovimientoResultadoDto> RevertirTransaccionAsync(
        long idTransaccion,
        ReversionRequest request,
        CancellationToken cancellationToken = default);
}
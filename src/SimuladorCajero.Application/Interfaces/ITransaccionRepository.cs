using SimuladorCajero.Application.DTOs;

namespace SimuladorCajero.Application.Interfaces;

public interface ITransaccionRepository
{
    Task<MovimientoResultadoDto> RegistrarDepositoAsync(
        MovimientoRequest request,
        CancellationToken cancellationToken = default);

    Task<MovimientoResultadoDto> RegistrarRetiroAsync(
        MovimientoRequest request,
        CancellationToken cancellationToken = default);

    Task<MovimientoResultadoDto> RevertirAsync(
        long idTransaccion,
        ReversionRequest request,
        CancellationToken cancellationToken = default);
}
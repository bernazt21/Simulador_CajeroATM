namespace SimuladorCajero.Application.DTOs;

public sealed record MovimientoRequest
{
    public int IdCuenta { get; init; }

    public decimal Monto { get; init; }
}
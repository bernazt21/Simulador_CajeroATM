namespace SimuladorCajero.Application.DTOs;

public sealed record MovimientoResultadoDto
{
    public long IdTransaccion { get; init; }

    public string Tipo { get; init; } = string.Empty;

    public decimal Monto { get; init; }

    public decimal SaldoAnterior { get; init; }

    public decimal SaldoPosterior { get; init; }

    public string Mensaje { get; init; } = string.Empty;

    public long? IdTransaccionOriginal { get; init; }
}
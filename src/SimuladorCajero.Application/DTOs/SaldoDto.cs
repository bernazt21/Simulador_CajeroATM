namespace SimuladorCajero.Application.DTOs;

public sealed record SaldoDto
{
    public int IdCuenta { get; init; }

    public string NumeroCuenta { get; init; } = string.Empty;

    public decimal Saldo { get; init; }

    public bool Activa { get; init; }
}
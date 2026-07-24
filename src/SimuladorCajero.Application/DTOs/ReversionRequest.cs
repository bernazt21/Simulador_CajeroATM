namespace SimuladorCajero.Application.DTOs;

public sealed record ReversionRequest
{
    public string Motivo { get; init; } = string.Empty;
}
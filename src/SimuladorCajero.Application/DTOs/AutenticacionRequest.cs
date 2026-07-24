namespace SimuladorCajero.Application.DTOs;

public sealed record AutenticacionRequest
{
    public string NumeroTarjeta { get; init; } = string.Empty;

    public string Nip { get; init; } = string.Empty;
}
namespace SimuladorCajero.Application.DTOs;

public sealed record TokenJwtDto
{
    public string Token { get; init; } = string.Empty;

    public DateTime ExpiracionUtc { get; init; }
}
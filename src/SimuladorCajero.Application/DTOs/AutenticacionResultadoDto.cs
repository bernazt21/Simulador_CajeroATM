namespace SimuladorCajero.Application.DTOs;

public sealed record AutenticacionResultadoDto
{
    public int IdTarjeta { get; init; }

    public int IdCuenta { get; init; }

    public string NumeroTarjeta { get; init; } = string.Empty;

    public string Token { get; init; } = string.Empty;

    public DateTime ExpiracionUtc { get; init; }

    public string Mensaje { get; init; } = string.Empty;
}
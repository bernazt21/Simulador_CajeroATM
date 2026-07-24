namespace SimuladorCajero.Application.DTOs;

public sealed record CambioNipRequest
{
    public int IdTarjeta { get; init; }

    public string NuevoNip { get; init; } = string.Empty;

    public string ConfirmacionNuevoNip { get; init; } = string.Empty;
}

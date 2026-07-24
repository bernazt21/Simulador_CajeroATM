namespace SimuladorCajero.Application.DTOs;

public sealed record CambioNipRequest
{
    public string NipActual { get; init; } = string.Empty;

    public string NuevoNip { get; init; } = string.Empty;

    public string ConfirmacionNuevoNip { get; init; } = string.Empty;
}
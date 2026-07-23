namespace SimuladorCajero.Domain.Entities;

public class Usuario
{
    public int IdUsuario { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string ApellidoPaterno { get; set; } = string.Empty;

    public string? ApellidoMaterno { get; set; }

    public string Correo { get; set; } = string.Empty;

    public bool Activo { get; set; }

    public DateTime FechaRegistro { get; set; }
}
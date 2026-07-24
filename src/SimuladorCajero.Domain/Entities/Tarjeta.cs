namespace SimuladorCajero.Domain.Entities;

public class Tarjeta
{
    private const byte MaximoIntentosFallidos = 3;

    public int IdTarjeta { get; set; }

    public int IdCuenta { get; set; }

    public string NumeroTarjeta { get; set; } = string.Empty;

    public string NipHash { get; private set; } = string.Empty;

    public bool Bloqueada { get; private set; }

    public byte IntentosFallidos { get; private set; }

    public DateTime FechaExpiracion { get; set; }

    public bool Activa { get; set; }

    public DateTime FechaCreacion { get; set; }

    public bool EstaVigente(DateTime fechaActual)
    {
        return Activa
            && !Bloqueada
            && FechaExpiracion.Date >= fechaActual.Date;
    }

    public void EstablecerNipHash(string nipHash)
    {
        if (string.IsNullOrWhiteSpace(nipHash))
        {
            throw new ArgumentException(
                "El hash del NIP es obligatorio.",
                nameof(nipHash));
        }

        NipHash = nipHash;
    }

    public void CargarEstadoSeguridad(
    byte intentosFallidos,
    bool bloqueada)
    {
        if (intentosFallidos > MaximoIntentosFallidos)
        {
            throw new ArgumentOutOfRangeException(
                nameof(intentosFallidos),
                "Los intentos fallidos no pueden superar tres.");
        }

        IntentosFallidos = intentosFallidos;
        Bloqueada =
            bloqueada ||
            intentosFallidos >= MaximoIntentosFallidos;
    }

    public void RegistrarIntentoFallido()
    {
        if (Bloqueada)
        {
            return;
        }

        IntentosFallidos++;

        if (IntentosFallidos >= MaximoIntentosFallidos)
        {
            IntentosFallidos = MaximoIntentosFallidos;
            Bloqueada = true;
        }
    }

    public void ReiniciarIntentosFallidos()
    {
        IntentosFallidos = 0;
    }

    public void Desbloquear()
    {
        Bloqueada = false;
        IntentosFallidos = 0;
    }
}
using SimuladorCajero.Domain.Enums;

namespace SimuladorCajero.Domain.Entities;

public class Transaccion
{
    public long IdTransaccion { get; set; }

    public int IdCuenta { get; set; }

    public TipoTransaccion Tipo { get; set; }

    public decimal Monto { get; set; }

    public decimal SaldoAnterior { get; set; }

    public decimal SaldoPosterior { get; set; }

    public EstadoTransaccion Estado { get; set; }

    public DateTime FechaTransaccion { get; set; }

    public long? IdTransaccionOriginal { get; set; }

    public string? MotivoReversion { get; set; }

    public Guid Referencia { get; set; }
}
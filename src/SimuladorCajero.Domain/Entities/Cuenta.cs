namespace SimuladorCajero.Domain.Entities;

public class Cuenta
{
    public int IdCuenta { get; set; }

    public int IdUsuario { get; set; }

    public string NumeroCuenta { get; set; } = string.Empty;

    public decimal Saldo { get; private set; }

    public bool Activa { get; set; }

    public DateTime FechaCreacion { get; set; }

    public void EstablecerSaldo(decimal saldo)
    {
        if (saldo < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(saldo),
                "El saldo no puede ser negativo.");
        }

        Saldo = saldo;
    }

    public void Depositar(decimal monto)
    {
        if (!Activa)
        {
            throw new InvalidOperationException(
                "La cuenta se encuentra inactiva.");
        }

        if (monto <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(monto),
                "El monto del depósito debe ser mayor que cero.");
        }

        Saldo += monto;
    }

    public void Retirar(decimal monto)
    {
        if (!Activa)
        {
            throw new InvalidOperationException(
                "La cuenta se encuentra inactiva.");
        }

        if (monto <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(monto),
                "El monto del retiro debe ser mayor que cero.");
        }

        if (Saldo < monto)
        {
            throw new InvalidOperationException(
                "Saldo insuficiente para realizar la operación.");
        }

        Saldo -= monto;
    }

    public bool TieneSaldoSuficiente(decimal monto)
    {
        return Activa && monto > 0 && Saldo >= monto;
    }
}
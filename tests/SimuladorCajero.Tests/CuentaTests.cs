using SimuladorCajero.Domain.Entities;

namespace SimuladorCajero.Tests;

public class CuentaTests
{
    [Fact]
    public void Depositar_MontoValido_AumentaSaldo()
    {
        // Arrange
        var cuenta = new Cuenta
        {
            IdCuenta = 1,
            IdUsuario = 1,
            NumeroCuenta = "1000000001",
            Activa = true
        };

        cuenta.EstablecerSaldo(1000m);

        // Act
        cuenta.Depositar(500m);

        // Assert
        Assert.Equal(1500m, cuenta.Saldo);
    }

    [Fact]
    public void Retirar_ConSaldoSuficiente_DisminuyeSaldo()
    {
        // Arrange
        var cuenta = new Cuenta
        {
            IdCuenta = 1,
            IdUsuario = 1,
            NumeroCuenta = "1000000001",
            Activa = true
        };

        cuenta.EstablecerSaldo(1000m);

        // Act
        cuenta.Retirar(400m);

        // Assert
        Assert.Equal(600m, cuenta.Saldo);
    }

    [Fact]
    public void Retirar_ConSaldoInsuficiente_LanzaExcepcion()
    {
        // Arrange
        var cuenta = new Cuenta
        {
            IdCuenta = 1,
            IdUsuario = 1,
            NumeroCuenta = "1000000001",
            Activa = true
        };

        cuenta.EstablecerSaldo(500m);

        // Act
        var excepcion = Assert.Throws<InvalidOperationException>(
            () => cuenta.Retirar(700m));

        // Assert
        Assert.Equal(
            "Saldo insuficiente para realizar la operación.",
            excepcion.Message);

        Assert.Equal(500m, cuenta.Saldo);
    }

    [Fact]
    public void Depositar_MontoNegativo_LanzaExcepcion()
    {
        var cuenta = new Cuenta
        {
            Activa = true
        };

        cuenta.EstablecerSaldo(1000m);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => cuenta.Depositar(-100m));

        Assert.Equal(1000m, cuenta.Saldo);
    }

    [Fact]
    public void Retirar_CuentaInactiva_LanzaExcepcion()
    {
        var cuenta = new Cuenta
        {
            Activa = false
        };

        cuenta.EstablecerSaldo(1000m);

        var excepcion = Assert.Throws<InvalidOperationException>(
            () => cuenta.Retirar(100m));

        Assert.Equal(
            "La cuenta se encuentra inactiva.",
            excepcion.Message);
    }
}
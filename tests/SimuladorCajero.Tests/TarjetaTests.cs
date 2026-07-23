using SimuladorCajero.Domain.Entities;

namespace SimuladorCajero.Tests;

public class TarjetaTests
{
    [Fact]
    public void RegistrarTresIntentosFallidos_BloqueaTarjeta()
    {
        // Arrange
        var tarjeta = new Tarjeta
        {
            IdTarjeta = 1,
            IdCuenta = 1,
            NumeroTarjeta = "4000000000000001",
            Activa = true,
            FechaExpiracion = DateTime.Today.AddYears(2)
        };

        // Act
        tarjeta.RegistrarIntentoFallido();
        tarjeta.RegistrarIntentoFallido();
        tarjeta.RegistrarIntentoFallido();

        // Assert
        Assert.True(tarjeta.Bloqueada);
        Assert.Equal((byte)3, tarjeta.IntentosFallidos);
    }

    [Fact]
    public void ReiniciarIntentosFallidos_ColocaContadorEnCero()
    {
        var tarjeta = new Tarjeta
        {
            Activa = true
        };

        tarjeta.RegistrarIntentoFallido();
        tarjeta.RegistrarIntentoFallido();

        tarjeta.ReiniciarIntentosFallidos();

        Assert.Equal((byte)0, tarjeta.IntentosFallidos);
        Assert.False(tarjeta.Bloqueada);
    }

    [Fact]
    public void EstaVigente_TarjetaActivaNoBloqueadaYNoExpirada_RegresaVerdadero()
    {
        var tarjeta = new Tarjeta
        {
            Activa = true,
            FechaExpiracion = DateTime.Today.AddYears(1)
        };

        var resultado = tarjeta.EstaVigente(DateTime.Today);

        Assert.True(resultado);
    }

    [Fact]
    public void EstaVigente_TarjetaExpirada_RegresaFalso()
    {
        var tarjeta = new Tarjeta
        {
            Activa = true,
            FechaExpiracion = DateTime.Today.AddDays(-1)
        };

        var resultado = tarjeta.EstaVigente(DateTime.Today);

        Assert.False(resultado);
    }

    [Fact]
    public void EstablecerNipHash_ValorVacio_LanzaExcepcion()
    {
        var tarjeta = new Tarjeta();

        Assert.Throws<ArgumentException>(
            () => tarjeta.EstablecerNipHash(""));
    }
}
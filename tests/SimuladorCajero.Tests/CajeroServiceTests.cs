using SimuladorCajero.Application.DTOs;
using SimuladorCajero.Application.Exceptions;
using SimuladorCajero.Application.Interfaces;
using SimuladorCajero.Application.Services;
using SimuladorCajero.Domain.Entities;

namespace SimuladorCajero.Tests;

public class CajeroServiceTests
{
    [Fact]
    public async Task ConsultarSaldo_CuentaValida_RegresaSaldo()
    {
        var cuentaRepository = new CuentaRepositoryFake
        {
            Saldo = new SaldoDto
            {
                IdCuenta = 1,
                NumeroCuenta = "1000000001",
                Saldo = 5000m,
                Activa = true
            }
        };

        var servicio = CrearServicio(
            cuentaRepository: cuentaRepository);

        var resultado = await servicio.ConsultarSaldoAsync(1);

        Assert.Equal(1, resultado.IdCuenta);
        Assert.Equal(5000m, resultado.Saldo);
        Assert.True(resultado.Activa);
    }

    [Fact]
    public async Task ConsultarSaldo_CuentaInexistente_LanzaExcepcion()
    {
        var cuentaRepository = new CuentaRepositoryFake
        {
            Saldo = null
        };

        var servicio = CrearServicio(
            cuentaRepository: cuentaRepository);

        var excepcion =
            await Assert.ThrowsAsync<ReglaNegocioException>(
                () => servicio.ConsultarSaldoAsync(99));

        Assert.Equal(
            "La cuenta no existe.",
            excepcion.Message);
    }

    [Fact]
    public async Task Depositar_DatosValidos_EjecutaRepositorio()
    {
        var cuentaRepository = CrearCuentaActiva(1000m);

        var transaccionRepository = new TransaccionRepositoryFake
        {
            ResultadoDeposito = new MovimientoResultadoDto
            {
                IdTransaccion = 1,
                Tipo = "DEPOSITO",
                Monto = 500m,
                SaldoAnterior = 1000m,
                SaldoPosterior = 1500m,
                Mensaje = "Depósito realizado correctamente."
            }
        };

        var servicio = CrearServicio(
            cuentaRepository,
            transaccionRepository);

        var request = new MovimientoRequest
        {
            IdCuenta = 1,
            Monto = 500m
        };

        var resultado = await servicio.DepositarAsync(request);

        Assert.True(transaccionRepository.DepositoEjecutado);
        Assert.Equal(500m, transaccionRepository.UltimoDeposito?.Monto);
        Assert.Equal(1500m, resultado.SaldoPosterior);
    }

    [Fact]
    public async Task Retirar_SaldoSuficiente_EjecutaRepositorio()
    {
        var cuentaRepository = CrearCuentaActiva(1000m);

        var transaccionRepository = new TransaccionRepositoryFake
        {
            ResultadoRetiro = new MovimientoResultadoDto
            {
                IdTransaccion = 2,
                Tipo = "RETIRO",
                Monto = 400m,
                SaldoAnterior = 1000m,
                SaldoPosterior = 600m,
                Mensaje = "Retiro realizado correctamente."
            }
        };

        var servicio = CrearServicio(
            cuentaRepository,
            transaccionRepository);

        var request = new MovimientoRequest
        {
            IdCuenta = 1,
            Monto = 400m
        };

        var resultado = await servicio.RetirarAsync(request);

        Assert.True(transaccionRepository.RetiroEjecutado);
        Assert.Equal(600m, resultado.SaldoPosterior);
    }

    [Fact]
    public async Task Retirar_SaldoInsuficiente_LanzaExcepcion()
    {
        var cuentaRepository = CrearCuentaActiva(500m);
        var transaccionRepository = new TransaccionRepositoryFake();

        var servicio = CrearServicio(
            cuentaRepository,
            transaccionRepository);

        var request = new MovimientoRequest
        {
            IdCuenta = 1,
            Monto = 700m
        };

        var excepcion =
            await Assert.ThrowsAsync<ReglaNegocioException>(
                () => servicio.RetirarAsync(request));

        Assert.Equal(
            "Saldo insuficiente para realizar la operación.",
            excepcion.Message);

        Assert.False(transaccionRepository.RetiroEjecutado);
    }

    [Fact]
    public async Task CambiarNip_DatosValidos_GeneraHashYActualizaTarjeta()
    {
        var tarjetaRepository = new TarjetaRepositoryFake();
        var nipHasher = new NipHasherFake();

        var servicio = CrearServicio(
            tarjetaRepository: tarjetaRepository,
            nipHasher: nipHasher);

        var request = new CambioNipRequest
        {
            IdTarjeta = 1,
            NuevoNip = "4321",
            ConfirmacionNuevoNip = "4321"
        };

        await servicio.CambiarNipAsync(request);

        Assert.Equal(1, tarjetaRepository.IdTarjetaActualizada);
        Assert.Equal("HASH_4321", tarjetaRepository.HashGuardado);
    }

    [Fact]
    public async Task CambiarNip_ConfirmacionDiferente_LanzaExcepcion()
    {
        var tarjetaRepository = new TarjetaRepositoryFake();

        var servicio = CrearServicio(
            tarjetaRepository: tarjetaRepository);

        var request = new CambioNipRequest
        {
            IdTarjeta = 1,
            NuevoNip = "4321",
            ConfirmacionNuevoNip = "1234"
        };

        var excepcion =
            await Assert.ThrowsAsync<ReglaNegocioException>(
                () => servicio.CambiarNipAsync(request));

        Assert.Equal(
            "El nuevo NIP y su confirmación no coinciden.",
            excepcion.Message);

        Assert.Null(tarjetaRepository.HashGuardado);
    }

    [Fact]
    public async Task RevertirTransaccion_SinMotivo_LanzaExcepcion()
    {
        var transaccionRepository = new TransaccionRepositoryFake();

        var servicio = CrearServicio(
            transaccionRepository: transaccionRepository);

        var request = new ReversionRequest
        {
            Motivo = ""
        };

        var excepcion =
            await Assert.ThrowsAsync<ReglaNegocioException>(
                () => servicio.RevertirTransaccionAsync(1, request));

        Assert.Equal(
            "El motivo de la reversión es obligatorio.",
            excepcion.Message);

        Assert.False(transaccionRepository.ReversionEjecutada);
    }

    [Fact]
    public async Task RevertirTransaccion_DatosValidos_EjecutaRepositorio()
    {
        var transaccionRepository = new TransaccionRepositoryFake
        {
            ResultadoReversion = new MovimientoResultadoDto
            {
                IdTransaccion = 5,
                Tipo = "REVERSO",
                Monto = 500m,
                SaldoAnterior = 1500m,
                SaldoPosterior = 1000m,
                IdTransaccionOriginal = 1,
                Mensaje = "Transacción revertida correctamente."
            }
        };

        var servicio = CrearServicio(
            transaccionRepository: transaccionRepository);

        var request = new ReversionRequest
        {
            Motivo = "Operación registrada por error"
        };

        var resultado =
            await servicio.RevertirTransaccionAsync(1, request);

        Assert.True(transaccionRepository.ReversionEjecutada);
        Assert.Equal(1, transaccionRepository.IdTransaccionRevertida);
        Assert.Equal(1, resultado.IdTransaccionOriginal);
    }

    private static CuentaRepositoryFake CrearCuentaActiva(
        decimal saldo)
    {
        return new CuentaRepositoryFake
        {
            Saldo = new SaldoDto
            {
                IdCuenta = 1,
                NumeroCuenta = "1000000001",
                Saldo = saldo,
                Activa = true
            }
        };
    }

    private static CajeroService CrearServicio(
        ICuentaRepository? cuentaRepository = null,
        ITransaccionRepository? transaccionRepository = null,
        ITarjetaRepository? tarjetaRepository = null,
        INipHasher? nipHasher = null)
    {
        return new CajeroService(
            cuentaRepository ?? new CuentaRepositoryFake(),
            transaccionRepository ?? new TransaccionRepositoryFake(),
            tarjetaRepository ?? new TarjetaRepositoryFake(),
            nipHasher ?? new NipHasherFake());
    }

    private sealed class CuentaRepositoryFake : ICuentaRepository
    {
        public SaldoDto? Saldo { get; set; }

        public Task<SaldoDto?> ObtenerSaldoAsync(
            int idCuenta,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Saldo);
        }
    }

    private sealed class TransaccionRepositoryFake
        : ITransaccionRepository
    {
        public bool DepositoEjecutado { get; private set; }

        public bool RetiroEjecutado { get; private set; }

        public bool ReversionEjecutada { get; private set; }

        public MovimientoRequest? UltimoDeposito { get; private set; }

        public long? IdTransaccionRevertida { get; private set; }

        public MovimientoResultadoDto ResultadoDeposito { get; set; }
            = new();

        public MovimientoResultadoDto ResultadoRetiro { get; set; }
            = new();

        public MovimientoResultadoDto ResultadoReversion { get; set; }
            = new();

        public Task<MovimientoResultadoDto> RegistrarDepositoAsync(
            MovimientoRequest request,
            CancellationToken cancellationToken = default)
        {
            DepositoEjecutado = true;
            UltimoDeposito = request;

            return Task.FromResult(ResultadoDeposito);
        }

        public Task<MovimientoResultadoDto> RegistrarRetiroAsync(
            MovimientoRequest request,
            CancellationToken cancellationToken = default)
        {
            RetiroEjecutado = true;

            return Task.FromResult(ResultadoRetiro);
        }

        public Task<MovimientoResultadoDto> RevertirAsync(
            long idTransaccion,
            ReversionRequest request,
            CancellationToken cancellationToken = default)
        {
            ReversionEjecutada = true;
            IdTransaccionRevertida = idTransaccion;

            return Task.FromResult(ResultadoReversion);
        }
    }

    private sealed class TarjetaRepositoryFake
        : ITarjetaRepository
    {
        public int? IdTarjetaActualizada { get; private set; }

        public string? HashGuardado { get; private set; }

        public Task<Tarjeta?> ObtenerPorNumeroAsync(
            string numeroTarjeta,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Tarjeta?>(null);
        }

        public Task CambiarNipAsync(
            int idTarjeta,
            string nuevoNipHash,
            CancellationToken cancellationToken = default)
        {
            IdTarjetaActualizada = idTarjeta;
            HashGuardado = nuevoNipHash;

            return Task.CompletedTask;
        }

        public Task ActualizarEstadoAsync(
            Tarjeta tarjeta,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class NipHasherFake : INipHasher
    {
        public string GenerarHash(string nip)
        {
            return $"HASH_{nip}";
        }

        public bool Verificar(
            string nip,
            string nipHash)
        {
            return nipHash == $"HASH_{nip}";
        }
    }
}
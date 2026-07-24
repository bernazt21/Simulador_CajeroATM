using SimuladorCajero.Application.DTOs;
using SimuladorCajero.Application.Exceptions;
using SimuladorCajero.Application.Interfaces;

namespace SimuladorCajero.Application.Services;

public sealed class CajeroService : ICajeroService
{
    private readonly ICuentaRepository _cuentaRepository;
    private readonly ITransaccionRepository _transaccionRepository;
    private readonly ITarjetaRepository _tarjetaRepository;
    private readonly INipHasher _nipHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public CajeroService(
        ICuentaRepository cuentaRepository,
        ITransaccionRepository transaccionRepository,
        ITarjetaRepository tarjetaRepository,
        INipHasher nipHasher,
        IJwtTokenService jwtTokenService)
    {
        _cuentaRepository = cuentaRepository
            ?? throw new ArgumentNullException(nameof(cuentaRepository));

        _transaccionRepository = transaccionRepository
            ?? throw new ArgumentNullException(nameof(transaccionRepository));

        _tarjetaRepository = tarjetaRepository
            ?? throw new ArgumentNullException(nameof(tarjetaRepository));

        _nipHasher = nipHasher
            ?? throw new ArgumentNullException(nameof(nipHasher));

        _jwtTokenService = jwtTokenService
            ?? throw new ArgumentNullException(nameof(jwtTokenService));
    }

    public async Task<SaldoDto> ConsultarSaldoAsync(
        int idCuenta,
        CancellationToken cancellationToken = default)
    {
        if (idCuenta <= 0)
        {
            throw new ReglaNegocioException(
                "El identificador de la cuenta no es válido.");
        }

        var cuenta = await _cuentaRepository.ObtenerSaldoAsync(
            idCuenta,
            cancellationToken);

        if (cuenta is null)
        {
            throw new ReglaNegocioException(
                "La cuenta no existe.");
        }

        if (!cuenta.Activa)
        {
            throw new ReglaNegocioException(
                "La cuenta se encuentra inactiva.");
        }

        return cuenta;
    }

    public async Task<MovimientoResultadoDto> DepositarAsync(
        MovimientoRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidarMovimiento(request, "depósito");

        await ConsultarSaldoAsync(
            request.IdCuenta,
            cancellationToken);

        return await _transaccionRepository.RegistrarDepositoAsync(
            request,
            cancellationToken);
    }

    public async Task<MovimientoResultadoDto> RetirarAsync(
        MovimientoRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidarMovimiento(request, "retiro");

        var cuenta = await ConsultarSaldoAsync(
            request.IdCuenta,
            cancellationToken);

        if (cuenta.Saldo < request.Monto)
        {
            throw new ReglaNegocioException(
                "Saldo insuficiente para realizar la operación.");
        }

        return await _transaccionRepository.RegistrarRetiroAsync(
            request,
            cancellationToken);
    }

    public async Task CambiarNipAsync(
    int idTarjeta,
    CambioNipRequest request,
    CancellationToken cancellationToken = default)
    {
        if (idTarjeta <= 0)
        {
            throw new ReglaNegocioException(
                "El identificador de la tarjeta no es válido.");
        }

        if (request is null)
        {
            throw new ReglaNegocioException(
                "Los datos para cambiar el NIP son obligatorios.");
        }

        ValidarNip(request.NipActual);
        ValidarNip(request.NuevoNip);

        if (request.NuevoNip != request.ConfirmacionNuevoNip)
        {
            throw new ReglaNegocioException(
                "El nuevo NIP y su confirmación no coinciden.");
        }

        if (request.NipActual == request.NuevoNip)
        {
            throw new ReglaNegocioException(
                "El nuevo NIP debe ser diferente al NIP actual.");
        }

        var tarjeta =
            await _tarjetaRepository.ObtenerPorIdAsync(
                idTarjeta,
                cancellationToken);

        if (tarjeta is null)
        {
            throw new ReglaNegocioException(
                "La tarjeta no existe.");
        }

        if (!tarjeta.Activa)
        {
            throw new ReglaNegocioException(
                "La tarjeta se encuentra inactiva.");
        }

        if (tarjeta.Bloqueada)
        {
            throw new ReglaNegocioException(
                "La tarjeta se encuentra bloqueada.");
        }

        if (tarjeta.FechaExpiracion.Date < DateTime.Today)
        {
            throw new ReglaNegocioException(
                "La tarjeta se encuentra vencida.");
        }

        var nipActualCorrecto = _nipHasher.Verificar(
            request.NipActual,
            tarjeta.NipHash);

        if (!nipActualCorrecto)
        {
            tarjeta.RegistrarIntentoFallido();

            await _tarjetaRepository.ActualizarEstadoAsync(
                tarjeta,
                cancellationToken);

            if (tarjeta.Bloqueada)
            {
                throw new ReglaNegocioException(
                    "El NIP actual es incorrecto. La tarjeta fue bloqueada después de tres intentos fallidos.");
            }

            throw new ReglaNegocioException(
                $"El NIP actual es incorrecto. Intentos fallidos: {tarjeta.IntentosFallidos} de 3.");
        }

        var nuevoNipHash = _nipHasher.GenerarHash(
            request.NuevoNip);

        await _tarjetaRepository.CambiarNipAsync(
            idTarjeta,
            nuevoNipHash,
            cancellationToken);

        if (tarjeta.IntentosFallidos > 0)
        {
            tarjeta.ReiniciarIntentosFallidos();

            await _tarjetaRepository.ActualizarEstadoAsync(
                tarjeta,
                cancellationToken);
        }
    }


    public async Task<MovimientoResultadoDto> RevertirTransaccionAsync(
        long idTransaccion,
        ReversionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (idTransaccion <= 0)
        {
            throw new ReglaNegocioException(
                "El identificador de la transacción no es válido.");
        }

        if (request is null ||
            string.IsNullOrWhiteSpace(request.Motivo))
        {
            throw new ReglaNegocioException(
                "El motivo de la reversión es obligatorio.");
        }

        if (request.Motivo.Length > 250)
        {
            throw new ReglaNegocioException(
                "El motivo de la reversión no puede superar 250 caracteres.");
        }

        return await _transaccionRepository.RevertirAsync(
            idTransaccion,
            request,
            cancellationToken);
    }

    public async Task<AutenticacionResultadoDto> AutenticarAsync(
        AutenticacionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ReglaNegocioException(
                "Los datos de autenticación son obligatorios.");
        }

        if (string.IsNullOrWhiteSpace(request.NumeroTarjeta) ||
            request.NumeroTarjeta.Length != 16 ||
            request.NumeroTarjeta.Any(
                caracter => !char.IsDigit(caracter)))
        {
            throw new ReglaNegocioException(
                "El número de tarjeta debe contener exactamente 16 dígitos.");
        }

        ValidarNip(request.Nip);

        var tarjeta =
            await _tarjetaRepository.ObtenerPorNumeroAsync(
                request.NumeroTarjeta,
                cancellationToken);

        if (tarjeta is null)
        {
            throw new ReglaNegocioException(
                "Número de tarjeta o NIP incorrectos.");
        }

        if (!tarjeta.Activa)
        {
            throw new ReglaNegocioException(
                "La tarjeta se encuentra inactiva.");
        }

        if (tarjeta.Bloqueada)
        {
            throw new ReglaNegocioException(
                "La tarjeta se encuentra bloqueada.");
        }

        if (tarjeta.FechaExpiracion.Date < DateTime.Today)
        {
            throw new ReglaNegocioException(
                "La tarjeta se encuentra vencida.");
        }

        var nipCorrecto = _nipHasher.Verificar(
            request.Nip,
            tarjeta.NipHash);

        if (!nipCorrecto)
        {
            tarjeta.RegistrarIntentoFallido();

            await _tarjetaRepository.ActualizarEstadoAsync(
                tarjeta,
                cancellationToken);

            if (tarjeta.Bloqueada)
            {
                throw new ReglaNegocioException(
                    "NIP incorrecto. La tarjeta fue bloqueada después de tres intentos fallidos.");
            }

            throw new ReglaNegocioException(
                $"NIP incorrecto. Intentos fallidos: {tarjeta.IntentosFallidos} de 3.");
        }

        if (tarjeta.IntentosFallidos > 0)
        {
            tarjeta.ReiniciarIntentosFallidos();

            await _tarjetaRepository.ActualizarEstadoAsync(
                tarjeta,
                cancellationToken);
        }

        var tokenJwt = _jwtTokenService.GenerarToken(
            tarjeta.IdTarjeta,
            tarjeta.IdCuenta);

        return new AutenticacionResultadoDto
        {
            IdTarjeta = tarjeta.IdTarjeta,
            IdCuenta = tarjeta.IdCuenta,
            NumeroTarjeta = tarjeta.NumeroTarjeta,
            Token = tokenJwt.Token,
            ExpiracionUtc = tokenJwt.ExpiracionUtc,
            Mensaje = "Autenticación realizada correctamente."
        };
    }

    private static void ValidarMovimiento(
        MovimientoRequest request,
        string tipoMovimiento)
    {
        if (request is null)
        {
            throw new ReglaNegocioException(
                $"Los datos del {tipoMovimiento} son obligatorios.");
        }

        if (request.IdCuenta <= 0)
        {
            throw new ReglaNegocioException(
                "El identificador de la cuenta no es válido.");
        }

        if (request.Monto <= 0)
        {
            throw new ReglaNegocioException(
                $"El monto del {tipoMovimiento} debe ser mayor que cero.");
        }
    }

    private static void ValidarNip(string nip)
    {
        if (string.IsNullOrWhiteSpace(nip))
        {
            throw new ReglaNegocioException(
                "El nuevo NIP es obligatorio.");
        }

        if (nip.Length != 4)
        {
            throw new ReglaNegocioException(
                "El NIP debe contener exactamente cuatro dígitos.");
        }

        if (nip.Any(caracter => !char.IsDigit(caracter)))
        {
            throw new ReglaNegocioException(
                "El NIP solamente puede contener números.");
        }
    }
}
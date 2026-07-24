using Microsoft.AspNetCore.Mvc;
using SimuladorCajero.Application.DTOs;
using SimuladorCajero.Application.Exceptions;
using SimuladorCajero.Application.Interfaces;

namespace SimuladorCajero.Api.Controllers;

[ApiController]
[Route("api/transacciones")]
public sealed class TransaccionesController : ControllerBase
{
    private readonly ICajeroService _cajeroService;

    public TransaccionesController(ICajeroService cajeroService)
    {
        _cajeroService = cajeroService
            ?? throw new ArgumentNullException(nameof(cajeroService));
    }

    /// <summary>
    /// Registra un depósito en una cuenta.
    /// </summary>
    [HttpPost("depositos")]
    [ProducesResponseType(
        typeof(MovimientoResultadoDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MovimientoResultadoDto>> Depositar(
        [FromBody] MovimientoRequest request,
        CancellationToken cancellationToken)
    {
        var errorValidacion = ValidarMovimiento(request);

        if (errorValidacion is not null)
        {
            return BadRequest(new
            {
                mensaje = errorValidacion
            });
        }

        try
        {
            var resultado = await _cajeroService.DepositarAsync(
                request,
                cancellationToken);

            return Ok(resultado);
        }
        catch (ReglaNegocioException exception)
        {
            return BadRequest(new
            {
                mensaje = exception.Message
            });
        }
    }

    /// <summary>
    /// Registra un retiro de una cuenta.
    /// </summary>
    [HttpPost("retiros")]
    [ProducesResponseType(
        typeof(MovimientoResultadoDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MovimientoResultadoDto>> Retirar(
        [FromBody] MovimientoRequest request,
        CancellationToken cancellationToken)
    {
        var errorValidacion = ValidarMovimiento(request);

        if (errorValidacion is not null)
        {
            return BadRequest(new
            {
                mensaje = errorValidacion
            });
        }

        try
        {
            var resultado = await _cajeroService.RetirarAsync(
                request,
                cancellationToken);

            return Ok(resultado);
        }
        catch (ReglaNegocioException exception)
        {
            return BadRequest(new
            {
                mensaje = exception.Message
            });
        }
    }

    private static string? ValidarMovimiento(
        MovimientoRequest request)
    {
        if (request.IdCuenta <= 0)
        {
            return "El identificador de la cuenta no es válido.";
        }

        if (request.Monto <= 0)
        {
            return "El monto debe ser mayor que cero.";
        }

        return null;
    }
}
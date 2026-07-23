using Microsoft.AspNetCore.Mvc;
using SimuladorCajero.Application.DTOs;
using SimuladorCajero.Application.Exceptions;
using SimuladorCajero.Application.Interfaces;

namespace SimuladorCajero.Api.Controllers;

[ApiController]
[Route("api/cuentas")]
public sealed class CuentasController : ControllerBase
{
    private readonly ICajeroService _cajeroService;

    public CuentasController(ICajeroService cajeroService)
    {
        _cajeroService = cajeroService
            ?? throw new ArgumentNullException(nameof(cajeroService));
    }

    /// <summary>
    /// Consulta el saldo disponible de una cuenta.
    /// </summary>
    /// <param name="idCuenta">Identificador de la cuenta.</param>
    /// <param name="cancellationToken">
    /// Token para cancelar la solicitud.
    /// </param>
    [HttpGet("{idCuenta:int}/saldo")]
    [ProducesResponseType(typeof(SaldoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaldoDto>> ConsultarSaldo(
        int idCuenta,
        CancellationToken cancellationToken)
    {
        if (idCuenta <= 0)
        {
            return BadRequest(new
            {
                mensaje = "El identificador de la cuenta no es válido."
            });
        }

        try
        {
            var resultado = await _cajeroService.ConsultarSaldoAsync(
                idCuenta,
                cancellationToken);

            return Ok(resultado);
        }
        catch (ReglaNegocioException exception)
        {
            return NotFound(new
            {
                mensaje = exception.Message
            });
        }
    }
}
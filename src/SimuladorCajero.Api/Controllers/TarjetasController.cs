using Microsoft.AspNetCore.Mvc;
using SimuladorCajero.Application.DTOs;
using SimuladorCajero.Application.Exceptions;
using SimuladorCajero.Application.Interfaces;

namespace SimuladorCajero.Api.Controllers;

[ApiController]
[Route("api/tarjetas")]
public sealed class TarjetasController : ControllerBase
{
    private readonly ICajeroService _cajeroService;

    public TarjetasController(ICajeroService cajeroService)
    {
        _cajeroService = cajeroService
            ?? throw new ArgumentNullException(nameof(cajeroService));
    }

    /// <summary>
    /// Cambia el NIP de una tarjeta activa y no bloqueada.
    /// </summary>
    /// <param name="idTarjeta">Identificador de la tarjeta.</param>
    /// <param name="request">Nuevo NIP y su confirmación.</param>
    /// <param name="cancellationToken">
    /// Token para cancelar la solicitud.
    /// </param>
    [HttpPut("{idTarjeta:int}/nip")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CambiarNip(
        int idTarjeta,
        [FromBody] CambioNipRequest request,
        CancellationToken cancellationToken)
    {
        if (idTarjeta <= 0)
        {
            return BadRequest(new
            {
                mensaje = "El identificador de la tarjeta no es válido."
            });
        }

        if (request is null)
        {
            return BadRequest(new
            {
                mensaje = "Los datos para cambiar el NIP son obligatorios."
            });
        }

        if (request.IdTarjeta != 0 &&
            request.IdTarjeta != idTarjeta)
        {
            return BadRequest(new
            {
                mensaje =
                    "El identificador de la ruta no coincide con el del cuerpo."
            });
        }

        var solicitud = request with
        {
            IdTarjeta = idTarjeta
        };

        try
        {
            await _cajeroService.CambiarNipAsync(
                solicitud,
                cancellationToken);

            return NoContent();
        }
        catch (ReglaNegocioException exception)
        {
            return BadRequest(new
            {
                mensaje = exception.Message
            });
        }
    }
}
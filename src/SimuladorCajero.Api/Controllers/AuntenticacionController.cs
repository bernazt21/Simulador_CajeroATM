using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimuladorCajero.Application.DTOs;
using SimuladorCajero.Application.Exceptions;
using SimuladorCajero.Application.Interfaces;

namespace SimuladorCajero.Api.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/autenticacion")]
public sealed class AutenticacionController : ControllerBase
{
    private readonly ICajeroService _cajeroService;

    public AutenticacionController(
        ICajeroService cajeroService)
    {
        _cajeroService = cajeroService
            ?? throw new ArgumentNullException(
                nameof(cajeroService));
    }

    /// <summary>
    /// Autentica una tarjeta mediante su número y NIP.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(
        typeof(AutenticacionResultadoDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AutenticacionResultadoDto>>
        Autenticar(
            [FromBody] AutenticacionRequest request,
            CancellationToken cancellationToken)
    {
        try
        {
            var resultado =
                await _cajeroService.AutenticarAsync(
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
}
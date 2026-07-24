using SimuladorCajero.Application.DTOs;

namespace SimuladorCajero.Application.Interfaces;

public interface IJwtTokenService
{
    TokenJwtDto GenerarToken(
        int idTarjeta,
        int idCuenta);
}
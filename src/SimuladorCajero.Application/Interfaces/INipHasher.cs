namespace SimuladorCajero.Application.Interfaces;

public interface INipHasher
{
    string GenerarHash(string nip);

    bool Verificar(string nip, string nipHash);
}
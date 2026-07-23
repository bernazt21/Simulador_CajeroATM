using SimuladorCajero.Application.Interfaces;

namespace SimuladorCajero.Infrastructure.Security;

public sealed class NipHasher : INipHasher
{
    public string GenerarHash(string nip)
    {
        if (string.IsNullOrWhiteSpace(nip))
        {
            throw new ArgumentException(
                "El NIP es obligatorio.",
                nameof(nip));
        }

        return BCrypt.Net.BCrypt.HashPassword(nip);
    }

    public bool Verificar(string nip, string nipHash)
    {
        if (string.IsNullOrWhiteSpace(nip) ||
            string.IsNullOrWhiteSpace(nipHash))
        {
            return false;
        }

        return BCrypt.Net.BCrypt.Verify(nip, nipHash);
    }
}
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using SimuladorCajero.Application.DTOs;
using SimuladorCajero.Application.Interfaces;

namespace SimuladorCajero.Infrastructure.Security;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;

    public JwtTokenService(
        string key,
        string issuer,
        string audience,
        int expirationMinutes)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException(
                "La clave JWT es obligatoria.",
                nameof(key));
        }

        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new ArgumentException(
                "El emisor JWT es obligatorio.",
                nameof(issuer));
        }

        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new ArgumentException(
                "La audiencia JWT es obligatoria.",
                nameof(audience));
        }

        if (expirationMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expirationMinutes),
                "La duración del JWT debe ser mayor que cero.");
        }

        _key = key;
        _issuer = issuer;
        _audience = audience;
        _expirationMinutes = expirationMinutes;
    }

    public TokenJwtDto GenerarToken(
        int idTarjeta,
        int idCuenta)
    {
        if (idTarjeta <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(idTarjeta),
                "El identificador de la tarjeta no es válido.");
        }

        if (idCuenta <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(idCuenta),
                "El identificador de la cuenta no es válido.");
        }

        var fechaActual = DateTime.UtcNow;

        var fechaExpiracion =
            fechaActual.AddMinutes(_expirationMinutes);

        var claims = new List<Claim>
        {
            new(
                JwtRegisteredClaimNames.Sub,
                idTarjeta.ToString()),

            new(
                "idTarjeta",
                idTarjeta.ToString()),

            new(
                "idCuenta",
                idCuenta.ToString()),

            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString()),

            new(
                JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(fechaActual)
                    .ToUnixTimeSeconds()
                    .ToString(),
                ClaimValueTypes.Integer64)
        };

        byte[] keyBytes;

        try
        {
            keyBytes = Convert.FromBase64String(_key);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                "La clave JWT no tiene un formato Base64 válido.",
                exception);
        }

        var securityKey =
            new SymmetricSecurityKey(keyBytes);

        var signingCredentials =
            new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: fechaActual,
            expires: fechaExpiracion,
            signingCredentials: signingCredentials);

        var token =
            new JwtSecurityTokenHandler()
                .WriteToken(jwt);

        return new TokenJwtDto
        {
            Token = token,
            ExpiracionUtc = fechaExpiracion
        };
    }
}
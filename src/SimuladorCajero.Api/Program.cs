using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using SimuladorCajero.Application.Interfaces;
using SimuladorCajero.Application.Services;
using SimuladorCajero.Infrastructure.Data;
using SimuladorCajero.Infrastructure.Repositories;
using SimuladorCajero.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// Agregar controladores.
builder.Services.AddControllers();

// Permitir pruebas desde clientes web externos durante desarrollo.
builder.Services.AddCors(options =>
{
    options.AddPolicy("ScalarClient", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Agregar documentación OpenAPI.
builder.Services.AddOpenApi();

// Obtener la cadena de conexión.
var connectionString =
    builder.Configuration.GetConnectionString("CajeroDb")
    ?? throw new InvalidOperationException(
        "No se encontró la cadena de conexión 'CajeroDb'.");

// Obtener la configuración JWT.
// Jwt:Key se obtiene desde User Secrets.
var jwtKey =
    builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "No se encontró la configuración 'Jwt:Key'.");

var jwtIssuer =
    builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "No se encontró la configuración 'Jwt:Issuer'.");

var jwtAudience =
    builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "No se encontró la configuración 'Jwt:Audience'.");

var jwtExpirationMinutes =
    builder.Configuration.GetValue<int>(
        "Jwt:ExpirationMinutes");

if (jwtExpirationMinutes <= 0)
{
    throw new InvalidOperationException(
        "La duración del JWT debe ser mayor que cero.");
}

// Convertir la clave almacenada en Base64.
byte[] jwtKeyBytes;

try
{
    jwtKeyBytes = Convert.FromBase64String(jwtKey);
}
catch (FormatException exception)
{
    throw new InvalidOperationException(
        "La clave JWT no tiene un formato Base64 válido.",
        exception);
}

// Configurar la validación de los tokens JWT.
builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(jwtKeyBytes),

                ValidateLifetime = true,

                // El token expira exactamente en el momento indicado.
                ClockSkew = TimeSpan.Zero
            };
    });

// Registrar autorización.
builder.Services.AddAuthorization();

// Registrar la fábrica de conexiones.
builder.Services.AddSingleton(
    new SqlConnectionFactory(connectionString));

// Registrar los repositorios.
builder.Services.AddScoped<ICuentaRepository, CuentaRepository>();
builder.Services.AddScoped<ITarjetaRepository, TarjetaRepository>();
builder.Services.AddScoped<ITransaccionRepository, TransaccionRepository>();

// Registrar seguridad del NIP.
builder.Services.AddScoped<INipHasher, NipHasher>();

// Registrar generación de tokens JWT.
builder.Services.AddSingleton<IJwtTokenService>(
    new JwtTokenService(
        jwtKey,
        jwtIssuer,
        jwtAudience,
        jwtExpirationMinutes));

// Registrar el servicio principal.
builder.Services.AddScoped<ICajeroService, CajeroService>();

var app = builder.Build();

// Configurar OpenAPI solamente durante desarrollo.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseCors("ScalarClient");
}

// Primero se valida la identidad mediante el JWT.
app.UseAuthentication();

// Después se comprueba la autorización.
app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/scalar"));

app.Run();
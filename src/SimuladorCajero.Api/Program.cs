using Scalar.AspNetCore;
using SimuladorCajero.Application.Interfaces;
using SimuladorCajero.Application.Services;
using SimuladorCajero.Infrastructure.Data;
using SimuladorCajero.Infrastructure.Repositories;
using SimuladorCajero.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// Agregar controladores.
builder.Services.AddControllers();



// Agregar documentación OpenAPI.
builder.Services.AddOpenApi();

// Obtener la cadena de conexión desde appsettings.Development.json.
var connectionString =
    builder.Configuration.GetConnectionString("CajeroDb")
    ?? throw new InvalidOperationException(
        "No se encontró la cadena de conexión 'CajeroDb'.");

// Registrar la fábrica de conexiones.
builder.Services.AddSingleton(
    new SqlConnectionFactory(connectionString));

// Registrar los repositorios.
builder.Services.AddScoped<ICuentaRepository, CuentaRepository>();
builder.Services.AddScoped<ITarjetaRepository, TarjetaRepository>();
builder.Services.AddScoped<ITransaccionRepository, TransaccionRepository>();

// Registrar seguridad del NIP.
builder.Services.AddScoped<INipHasher, NipHasher>();

// Registrar el servicio principal.
builder.Services.AddScoped<ICajeroService, CajeroService>();

// Construir la aplicación.
// Las dependencias deben registrarse antes de esta línea.
var app = builder.Build();

// Configurar OpenAPI solamente durante el desarrollo.
if (app.Environment.IsDevelopment())
{
     // Documento OpenAPI en formato JSON.
    app.MapOpenApi();
    // Interfaz visual para consultar y probar los endpoints.
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/scalar"));

app.Run();
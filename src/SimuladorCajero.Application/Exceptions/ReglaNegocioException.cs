namespace SimuladorCajero.Application.Exceptions;

public class ReglaNegocioException : Exception
{
    public ReglaNegocioException(string mensaje)
        : base(mensaje)
    {
    }

    public ReglaNegocioException(
        string mensaje,
        Exception excepcionInterna)
        : base(mensaje, excepcionInterna)
    {
    }
}
namespace Proyecto_Final.Servicios.Autenticacion;

public enum TipoResultadoAutenticacion
{
    Exito,
    CredencialesInvalidas,
    Bloqueado,
    RequiereCambioContrasena,
    NoAutenticado,
    Validacion
}

public class ResultadoAutenticacion
{
    public TipoResultadoAutenticacion Tipo { get; init; }

    public string? Mensaje { get; init; }

    public Dictionary<string, List<string>> Errores { get; init; } =
        [];

    public static ResultadoAutenticacion Correcto(
        string? mensaje = null)
    {
        return new ResultadoAutenticacion
        {
            Tipo = TipoResultadoAutenticacion.Exito,
            Mensaje = mensaje
        };
    }

    public static ResultadoAutenticacion CredencialesInvalidas(
        string mensaje)
    {
        return new ResultadoAutenticacion
        {
            Tipo = TipoResultadoAutenticacion.CredencialesInvalidas,
            Mensaje = mensaje
        };
    }

    public static ResultadoAutenticacion Bloqueado(
        string mensaje)
    {
        return new ResultadoAutenticacion
        {
            Tipo = TipoResultadoAutenticacion.Bloqueado,
            Mensaje = mensaje
        };
    }

    public static ResultadoAutenticacion RequiereCambioContrasena()
    {
        return new ResultadoAutenticacion
        {
            Tipo = TipoResultadoAutenticacion.RequiereCambioContrasena
        };
    }

    public static ResultadoAutenticacion NoAutenticado()
    {
        return new ResultadoAutenticacion
        {
            Tipo = TipoResultadoAutenticacion.NoAutenticado
        };
    }

    public static ResultadoAutenticacion Validacion(
        Dictionary<string, List<string>> errores)
    {
        return new ResultadoAutenticacion
        {
            Tipo = TipoResultadoAutenticacion.Validacion,
            Errores = errores
        };
    }
}
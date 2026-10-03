namespace Proyecto_Final.Servicios.Usuarios;

public enum TipoResultadoUsuario
{
    Exito,
    Error,
    Advertencia,
    Validacion,
    NoAutenticado,
    Prohibido
}

public class ResultadoUsuario<T>
{
    public TipoResultadoUsuario Tipo { get; init; }

    public T? Datos { get; init; }

    public string? Mensaje { get; init; }

    public Dictionary<string, List<string>> Errores { get; init; } =
        [];

    public static ResultadoUsuario<T> Correcto(
        T datos,
        string? mensaje = null)
    {
        return new ResultadoUsuario<T>
        {
            Tipo = TipoResultadoUsuario.Exito,
            Datos = datos,
            Mensaje = mensaje
        };
    }

    public static ResultadoUsuario<T> Error(
        string mensaje)
    {
        return new ResultadoUsuario<T>
        {
            Tipo = TipoResultadoUsuario.Error,
            Mensaje = mensaje
        };
    }

    public static ResultadoUsuario<T> Advertencia(
        string mensaje)
    {
        return new ResultadoUsuario<T>
        {
            Tipo = TipoResultadoUsuario.Advertencia,
            Mensaje = mensaje
        };
    }

    public static ResultadoUsuario<T> Validacion(
        string campo,
        string mensaje)
    {
        return new ResultadoUsuario<T>
        {
            Tipo = TipoResultadoUsuario.Validacion,
            Errores = new Dictionary<string, List<string>>
            {
                [campo] = [mensaje]
            }
        };
    }

    public static ResultadoUsuario<T> Validacion(
        Dictionary<string, List<string>> errores)
    {
        return new ResultadoUsuario<T>
        {
            Tipo = TipoResultadoUsuario.Validacion,
            Errores = errores
        };
    }

    public static ResultadoUsuario<T> NoAutenticado()
    {
        return new ResultadoUsuario<T>
        {
            Tipo = TipoResultadoUsuario.NoAutenticado
        };
    }

    public static ResultadoUsuario<T> Prohibido()
    {
        return new ResultadoUsuario<T>
        {
            Tipo = TipoResultadoUsuario.Prohibido
        };
    }
}
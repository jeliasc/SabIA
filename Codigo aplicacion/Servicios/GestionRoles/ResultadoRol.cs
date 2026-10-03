namespace Proyecto_Final.Servicios.GestionRoles;

public enum TipoResultadoRol
{
    Correcto,
    Validacion,
    Advertencia,
    Error
}

public class ResultadoRol<T>
{
    public TipoResultadoRol Tipo { get; private set; }

    public T? Datos { get; private set; }

    public string? Mensaje { get; private set; }

    public Dictionary<string, List<string>> Errores { get; private set; }
        = [];

    private ResultadoRol()
    {
    }

    // RESULTADO CORRECTO
    public static ResultadoRol<T> Correcto(
        T datos,
        string? mensaje = null)
    {
        return new ResultadoRol<T>
        {
            Tipo = TipoResultadoRol.Correcto,
            Datos = datos,
            Mensaje = mensaje
        };
    }

    // ERROR DE VALIDACIÓN
    public static ResultadoRol<T> Validacion(
        string campo,
        string mensaje)
    {
        return Validacion(
            new Dictionary<string, List<string>>
            {
                [campo] = [mensaje]
            }
        );
    }

    // ERRORES DE VALIDACIÓN
    public static ResultadoRol<T> Validacion(
        Dictionary<string, List<string>> errores)
    {
        return new ResultadoRol<T>
        {
            Tipo = TipoResultadoRol.Validacion,
            Errores = errores
        };
    }

    public static ResultadoRol<T> Advertencia(
    string mensaje)
    {
        return new ResultadoRol<T>
        {
            Tipo = TipoResultadoRol.Advertencia,
            Mensaje = mensaje
        };
    }

    // ERROR GENERAL
    public static ResultadoRol<T> Error(string mensaje)
    {
        return new ResultadoRol<T>
        {
            Tipo = TipoResultadoRol.Error,
            Mensaje = mensaje
        };
    }
}
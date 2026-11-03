namespace Proyecto_Final.Servicios.Comunes;

public enum TipoResultadoOperacion
{
    Correcto,
    Error,
    Validacion,
    Prohibido,
    NoEncontrado,
    Conflicto
}

public class ResultadoOperacion
{
    public bool Exitoso { get; init; }
    public TipoResultadoOperacion Tipo { get; init; } = TipoResultadoOperacion.Error;
    public string Mensaje { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> Errores { get; init; } =
        new Dictionary<string, string>();

    public static ResultadoOperacion Correcto(string mensaje) =>
        new()
        {
            Exitoso = true,
            Tipo = TipoResultadoOperacion.Correcto,
            Mensaje = mensaje
        };

    public static ResultadoOperacion Error(string mensaje) =>
        new()
        {
            Exitoso = false,
            Tipo = TipoResultadoOperacion.Error,
            Mensaje = mensaje
        };

    public static ResultadoOperacion Validacion(
        string campo,
        string mensaje) =>
        new()
        {
            Exitoso = false,
            Tipo = TipoResultadoOperacion.Validacion,
            Mensaje = "Revise la información ingresada.",
            Errores = new Dictionary<string, string>
            {
                [campo] = mensaje
            }
        };

    public static ResultadoOperacion Validacion(
        IReadOnlyDictionary<string, string> errores) =>
        new()
        {
            Exitoso = false,
            Tipo = TipoResultadoOperacion.Validacion,
            Mensaje = "Revise la información ingresada.",
            Errores = errores
        };

    public static ResultadoOperacion NoEncontrado(string mensaje) => new()
    {
        Exitoso = false, Tipo = TipoResultadoOperacion.NoEncontrado, Mensaje = mensaje
    };

    public static ResultadoOperacion Conflicto(string mensaje) => new()
    {
        Exitoso = false, Tipo = TipoResultadoOperacion.Conflicto, Mensaje = mensaje
    };
}

public sealed class ResultadoOperacion<T> : ResultadoOperacion
{
    public T? Datos { get; init; }

    public static ResultadoOperacion<T> Correcto(
        T datos,
        string mensaje = "Operación realizada correctamente.") =>
        new()
        {
            Exitoso = true,
            Tipo = TipoResultadoOperacion.Correcto,
            Mensaje = mensaje,
            Datos = datos
        };

    public new static ResultadoOperacion<T> Error(string mensaje) =>
        new()
        {
            Exitoso = false,
            Tipo = TipoResultadoOperacion.Error,
            Mensaje = mensaje
        };

    public new static ResultadoOperacion<T> Validacion(
        string campo,
        string mensaje) =>
        new()
        {
            Exitoso = false,
            Tipo = TipoResultadoOperacion.Validacion,
            Mensaje = "Revise la información ingresada.",
            Errores = new Dictionary<string, string>
            {
                [campo] = mensaje
            }
        };

    public new static ResultadoOperacion<T> Validacion(
        IReadOnlyDictionary<string, string> errores) =>
        new()
        {
            Exitoso = false,
            Tipo = TipoResultadoOperacion.Validacion,
            Mensaje = "Revise la información ingresada.",
            Errores = errores
        };

    public new static ResultadoOperacion<T> NoEncontrado(string mensaje) => new()
    {
        Exitoso = false, Tipo = TipoResultadoOperacion.NoEncontrado, Mensaje = mensaje
    };

    public new static ResultadoOperacion<T> Conflicto(string mensaje) => new()
    {
        Exitoso = false, Tipo = TipoResultadoOperacion.Conflicto, Mensaje = mensaje
    };
}

namespace Proyecto_Final.Servicios.Comunes;

public class ResultadoOperacion
{
    public bool Exitoso { get; init; }
    public string Mensaje { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> Errores { get; init; } =
        new Dictionary<string, string>();

    public static ResultadoOperacion Correcto(string mensaje) =>
        new()
        {
            Exitoso = true,
            Mensaje = mensaje
        };

    public static ResultadoOperacion Error(string mensaje) =>
        new()
        {
            Exitoso = false,
            Mensaje = mensaje
        };

    public static ResultadoOperacion Validacion(
        string campo,
        string mensaje) =>
        new()
        {
            Exitoso = false,
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
            Mensaje = "Revise la información ingresada.",
            Errores = errores
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
            Mensaje = mensaje,
            Datos = datos
        };

    public new static ResultadoOperacion<T> Error(string mensaje) =>
        new()
        {
            Exitoso = false,
            Mensaje = mensaje
        };

    public new static ResultadoOperacion<T> Validacion(
        string campo,
        string mensaje) =>
        new()
        {
            Exitoso = false,
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
            Mensaje = "Revise la información ingresada.",
            Errores = errores
        };
}

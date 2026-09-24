using System.Security.Cryptography;

namespace Proyecto_Final.Seguridad;

public static class GeneradorContrasenaTemporal
{
    // GENERAR CONTRASEÑA TEMPORAL
    public static string Generar()
    {
        const string mayusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string minusculas = "abcdefghijkmnopqrstuvwxyz";
        const string numeros = "23456789";
        const string especiales = "!@$%*-_";

        var caracteres = new List<char>
        {
            ObtenerCaracterAleatorio(mayusculas),
            ObtenerCaracterAleatorio(minusculas),
            ObtenerCaracterAleatorio(numeros),
            ObtenerCaracterAleatorio(especiales)
        };

        const string todos =
            mayusculas +
            minusculas +
            numeros +
            especiales;

        // Completar hasta 10 caracteres.
        while (caracteres.Count < 10)
        {
            caracteres.Add(
                ObtenerCaracterAleatorio(todos)
            );
        }

        // Mezclar los caracteres para evitar un patrón predecible.
        return new string(
            caracteres
                .OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue))
                .ToArray()
        );
    }

    // OBTENER CARÁCTER ALEATORIO SEGURO
    private static char ObtenerCaracterAleatorio(string caracteres)
    {
        var indice =
            RandomNumberGenerator.GetInt32(caracteres.Length);

        return caracteres[indice];
    }
}
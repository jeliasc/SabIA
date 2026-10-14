using System.Globalization;
using System.Text;

namespace Proyecto_Final.Servicios.GestionCuestionarios;

// Compara las respuestas directas de los alumnos con las respuestas
// aceptadas por el docente. Ignora mayúsculas, tildes, diéresis, espacios
// repetidos y la puntuación al inicio o al final, para que una respuesta
// correcta no se califique como incorrecta por detalles de escritura.
// La ñ se conserva porque es una letra distinta de la n.
public static class ComparadorRespuestas
{
    private const char MarcadorEnie = '\uE000';
    private const char MarcadorEnieMayuscula = '\uE001';

    private static readonly char[] PuntuacionExtremos =
    [
        '.', ',', ';', ':', '!', '¡', '?', '¿', '"', '\'', '«', '»', '“', '”', '(', ')'
    ];

    public static bool SonEquivalentes(string? aceptada, string? respuesta)
    {
        var a = Normalizar(aceptada);
        var b = Normalizar(respuesta);

        return a.Length > 0 && string.Equals(a, b, StringComparison.Ordinal);
    }

    public static string Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return string.Empty;

        // Se normaliza primero a la forma compuesta para que una ñ escrita
        // como "n + tilde combinable" se trate igual que la ñ normal.
        var texto = valor.Normalize(NormalizationForm.FormC)
            .Replace('ñ', MarcadorEnie)
            .Replace('Ñ', MarcadorEnieMayuscula)
            .Normalize(NormalizationForm.FormD);

        var constructor = new StringBuilder(texto.Length);
        var espacioPendiente = false;

        foreach (var caracter in texto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsWhiteSpace(caracter))
            {
                espacioPendiente = constructor.Length > 0;
                continue;
            }

            if (espacioPendiente)
            {
                constructor.Append(' ');
                espacioPendiente = false;
            }

            constructor.Append(caracter switch
            {
                MarcadorEnie or MarcadorEnieMayuscula => 'ñ',
                _ => char.ToLowerInvariant(caracter)
            });
        }

        return constructor.ToString().Trim(PuntuacionExtremos).Trim();
    }
}

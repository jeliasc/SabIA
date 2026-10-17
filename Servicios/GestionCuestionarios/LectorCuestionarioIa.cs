using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Proyecto_Final.Models;
using Proyecto_Final.ViewModels.Cuestionarios;

namespace Proyecto_Final.Servicios.GestionCuestionarios;

// Instrucción de formato que se agrega a lo que pide el docente cuando
// solicita un cuestionario a la IA, para poder importarlo después.
public static class FormatoCuestionarioIa
{
    public const string Instrucciones =
        "Formato obligatorio de la respuesta: devuelve únicamente JSON válido, sin texto antes ni después " +
        "y sin bloques de código, con esta estructura: " +
        "{\"preguntas\":[{\"tipo\":\"seleccion_unica\",\"enunciado\":\"...\"," +
        "\"opciones\":[\"...\",\"...\",\"...\",\"...\"],\"correctas\":[\"texto exacto de la opción correcta\"]," +
        "\"explicacion\":\"...\"}]}. " +
        "Tipos permitidos: seleccion_unica, seleccion_multiple (varias opciones correctas), " +
        "verdadero_falso (opciones [\"Verdadero\",\"Falso\"]) y respuesta_directa " +
        "(sin opciones; en correctas escribe las respuestas aceptadas, de una a tres palabras). " +
        "Verifica que cada respuesta marcada como correcta sea realmente correcta.";

    public static string Agregar(string? instruccionesDocente) =>
        string.IsNullOrWhiteSpace(instruccionesDocente)
            ? Instrucciones
            : instruccionesDocente.Trim() + "\n\n" + Instrucciones;
}

// Convierte el resultado de la IA en preguntas para la vista previa de
// importación. Primero intenta leer el JSON pedido; si la IA respondió en
// texto libre ("1. ¿...? A) ... B) ... Respuesta correcta: B"), lo
// interpreta. Lo que no pueda determinar queda marcado para que el docente
// lo corrija antes de importar.
public static class LectorCuestionarioIa
{
    public const string AvisoSinRespuesta = "No se identificó la respuesta correcta. Corríjala antes de importar.";

    private static readonly RegexOptions Opciones =
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

    public static List<PreguntaImportadaIa> Analizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return [];

        var desdeJson = LeerJson(texto);
        return desdeJson.Count > 0 ? desdeJson : LeerTexto(texto);
    }

    // =================================================================
    // JSON
    // =================================================================

    private static List<PreguntaImportadaIa> LeerJson(string texto)
    {
        var limpio = Regex.Replace(texto, "```[a-zA-Z]*", string.Empty).Trim();
        var inicio = limpio.IndexOfAny(['{', '[']);
        var fin = Math.Max(limpio.LastIndexOf('}'), limpio.LastIndexOf(']'));

        if (inicio < 0 || fin <= inicio)
            return [];

        try
        {
            using var documento = JsonDocument.Parse(limpio[inicio..(fin + 1)]);
            var raiz = documento.RootElement;

            JsonElement lista;
            if (raiz.ValueKind == JsonValueKind.Array)
                lista = raiz;
            else if (raiz.ValueKind == JsonValueKind.Object &&
                     Propiedad(raiz, "preguntas", "questions", "cuestionario", "items") is { ValueKind: JsonValueKind.Array } encontrada)
                lista = encontrada;
            else
                return [];

            var preguntas = new List<PreguntaImportadaIa>();
            foreach (var elemento in lista.EnumerateArray())
            {
                if (elemento.ValueKind != JsonValueKind.Object)
                    continue;

                var enunciado = Texto(Propiedad(elemento, "enunciado", "pregunta", "question", "texto"));
                if (string.IsNullOrWhiteSpace(enunciado))
                    continue;

                var opciones = Lista(Propiedad(elemento, "opciones", "options", "alternativas"))
                    .Select(QuitarLetra)
                    .Where(x => x.Length > 0)
                    .ToList();

                var correctas = Lista(Propiedad(elemento,
                        "correctas", "respuestas_correctas", "respuestas", "respuesta_correcta",
                        "correcta", "respuesta", "answer", "answers"))
                    .Select(x => ResolverRespuesta(x, opciones))
                    .Where(x => x.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var tipoTexto = Texto(Propiedad(elemento, "tipo", "type"));
                preguntas.Add(Construir(
                    enunciado,
                    opciones,
                    correctas,
                    Texto(Propiedad(elemento, "explicacion", "retroalimentacion", "justificacion", "explanation")),
                    tipoTexto));
            }

            return preguntas;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static JsonElement? Propiedad(JsonElement objeto, params string[] nombres)
    {
        foreach (var propiedad in objeto.EnumerateObject())
        {
            var nombre = SinTildes(propiedad.Name).Replace(" ", "_");
            if (nombres.Any(x => string.Equals(x, nombre, StringComparison.OrdinalIgnoreCase)))
                return propiedad.Value;
        }

        return null;
    }

    private static string Texto(JsonElement? valor) =>
        valor is { ValueKind: JsonValueKind.String } texto ? texto.GetString()?.Trim() ?? string.Empty
        : valor is { ValueKind: JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False } otro ? otro.ToString()
        : string.Empty;

    private static List<string> Lista(JsonElement? valor)
    {
        if (valor is not { } elemento)
            return [];

        if (elemento.ValueKind == JsonValueKind.Array)
        {
            return elemento.EnumerateArray()
                .Select(x => x.ValueKind == JsonValueKind.Object
                    ? Texto(Propiedad(x, "texto", "text", "opcion", "valor"))
                    : Texto(x))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }

        var simple = Texto(elemento);
        return string.IsNullOrWhiteSpace(simple) ? [] : [simple];
    }

    // =================================================================
    // TEXTO LIBRE
    // =================================================================

    private static List<PreguntaImportadaIa> LeerTexto(string texto)
    {
        var limpio = texto.Replace("\r\n", "\n").Replace("**", string.Empty).Replace("__", string.Empty);

        // Clave final: "Respuestas correctas: 1. B 2. B 3. C"
        var clave = new Dictionary<int, string>();
        var final = Regex.Match(
            limpio,
            @"Respuestas\s+correctas\s*:?\s*((?:\d{1,2}\s*[\.\):\-]\s*[A-F](?:\s*,\s*[A-F])*\s*[,;]?\s*)+)\s*$",
            Opciones);

        if (final.Success)
        {
            foreach (Match par in Regex.Matches(final.Groups[1].Value, @"(\d{1,2})\s*[\.\):\-]\s*([A-F](?:\s*,\s*[A-F])*)", Opciones))
                clave[int.Parse(par.Groups[1].Value, CultureInfo.InvariantCulture)] = par.Groups[2].Value;

            limpio = limpio[..final.Index];
        }

        // Inicio de cada pregunta: "1. ", "2) ", "Pregunta 3: " en orden.
        var candidatos = Regex.Matches(limpio, @"(?<![^\s\-])(?:pregunta\s*)?(\d{1,2})[\.\):]\s+", Opciones);
        var inicios = new List<(int Numero, int Indice, int Largo)>();
        var esperado = 1;

        foreach (Match candidato in candidatos)
        {
            if (int.Parse(candidato.Groups[1].Value, CultureInfo.InvariantCulture) != esperado)
                continue;

            inicios.Add((esperado, candidato.Index, candidato.Length));
            esperado++;
        }

        var preguntas = new List<PreguntaImportadaIa>();
        for (var i = 0; i < inicios.Count; i++)
        {
            var desde = inicios[i].Indice + inicios[i].Largo;
            var hasta = i + 1 < inicios.Count ? inicios[i + 1].Indice : limpio.Length;
            var bloque = limpio[desde..hasta];

            var pregunta = LeerBloque(bloque, clave.GetValueOrDefault(inicios[i].Numero));
            if (pregunta is not null)
                preguntas.Add(pregunta);
        }

        return preguntas;
    }

    private static PreguntaImportadaIa? LeerBloque(string bloque, string? claveFinal)
    {
        bloque = bloque.Trim().TrimEnd('-').Trim();

        string? explicacion = null;
        var explicacionEncontrada = Regex.Match(
            bloque, @"(?:Explicaci[oó]n|Retroalimentaci[oó]n|Justificaci[oó]n)\s*:\s*(.+)$",
            Opciones | RegexOptions.Singleline);
        if (explicacionEncontrada.Success)
        {
            explicacion = Compactar(explicacionEncontrada.Groups[1].Value).TrimEnd('-').Trim();
            bloque = bloque[..explicacionEncontrada.Index];
        }

        string? respuesta = null;
        var respuestaEncontrada = Regex.Match(
            bloque, @"Respuestas?(?:\s+correctas?)?\s*:\s*(.+)$",
            Opciones | RegexOptions.Singleline);
        if (respuestaEncontrada.Success)
        {
            respuesta = Compactar(respuestaEncontrada.Groups[1].Value).TrimEnd('-').Trim();
            bloque = bloque[..respuestaEncontrada.Index];
        }

        respuesta ??= claveFinal;

        // Opciones "A) ...", "b. ...", en orden alfabético desde la A.
        var marcas = Regex.Matches(bloque, @"(?<![^\s\(])([A-F])[\)\.]\s+", Opciones);
        var secuencia = new List<Match>();
        foreach (Match marca in marcas)
        {
            var letra = char.ToUpperInvariant(marca.Groups[1].Value[0]);
            if (letra == (char)('A' + secuencia.Count))
                secuencia.Add(marca);
        }

        var opciones = new List<string>();
        string enunciado;

        if (secuencia.Count >= 2)
        {
            enunciado = bloque[..secuencia[0].Index];
            for (var i = 0; i < secuencia.Count; i++)
            {
                var desde = secuencia[i].Index + secuencia[i].Length;
                var hasta = i + 1 < secuencia.Count ? secuencia[i + 1].Index : bloque.Length;
                opciones.Add(Compactar(bloque[desde..hasta]).TrimEnd('-').Trim());
            }
        }
        else
        {
            enunciado = bloque;
        }

        enunciado = Compactar(enunciado).TrimEnd('-').Trim();
        if (enunciado.Length == 0)
            return null;

        var correctas = new List<string>();
        if (!string.IsNullOrWhiteSpace(respuesta))
        {
            var soloLetras = Regex.Match(respuesta, @"^([A-F])(?:\s*(?:,|y|e)\s*([A-F]))*(?:[\)\.]?)(?:\s|$)", Opciones);
            if (opciones.Count > 0 && soloLetras.Success && respuesta.Trim().Length <= 12)
            {
                correctas.AddRange(Regex.Matches(soloLetras.Value, "[A-F]", Opciones)
                    .Select(x => ResolverRespuesta(x.Value, opciones)));
            }
            else if (opciones.Count > 0 && soloLetras.Success)
            {
                // "B) Quintos": se toma la letra.
                correctas.Add(ResolverRespuesta(soloLetras.Groups[1].Value, opciones));
            }
            else if (opciones.Count > 0)
            {
                correctas.Add(ResolverRespuesta(respuesta, opciones));
            }
            else
            {
                correctas.AddRange(respuesta
                    .Split(['/', ';', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(x => x.Trim('.', ' ')));
            }
        }

        return Construir(
            enunciado,
            opciones,
            correctas.Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            explicacion,
            tipoTexto: null);
    }

    // =================================================================
    // APOYO
    // =================================================================

    private static PreguntaImportadaIa Construir(
        string enunciado,
        List<string> opciones,
        List<string> correctas,
        string? explicacion,
        string? tipoTexto)
    {
        var tipoIndicado = SinTildes(tipoTexto ?? string.Empty).ToLowerInvariant();
        var esVerdaderoFalso =
            tipoIndicado.Contains("verdadero") || tipoIndicado.Contains("true") ||
            (opciones.Count == 2 && opciones.All(EsVerdaderoOFalso)) ||
            (opciones.Count == 0 && correctas.Count == 1 && EsVerdaderoOFalso(correctas[0]));

        TipoPregunta tipo;
        if (esVerdaderoFalso)
        {
            tipo = TipoPregunta.VerdaderoFalso;
            correctas = correctas
                .Select(x => SinTildes(x).StartsWith("v", StringComparison.OrdinalIgnoreCase) ||
                             x.StartsWith("t", StringComparison.OrdinalIgnoreCase) ? "Verdadero" : "Falso")
                .Distinct()
                .ToList();
            opciones = ["Verdadero", "Falso"];
        }
        else if (opciones.Count < 2 ||
                 tipoIndicado.Contains("directa") || tipoIndicado.Contains("corta") ||
                 tipoIndicado.Contains("abierta") || tipoIndicado.Contains("texto"))
        {
            tipo = TipoPregunta.TextoCorto;
            opciones = [];
        }
        else if (correctas.Count > 1 || tipoIndicado.Contains("multiple"))
        {
            tipo = TipoPregunta.SeleccionMultiple;
        }
        else
        {
            tipo = TipoPregunta.SeleccionUnica;
        }

        // Para selección, la respuesta debe coincidir con una opción.
        if (opciones.Count > 0)
        {
            correctas = correctas
                .Select(c => opciones.FirstOrDefault(o => ComparadorRespuestas.SonEquivalentes(o, c)) ?? c)
                .Where(c => opciones.Contains(c))
                .ToList();
        }

        return new PreguntaImportadaIa
        {
            Incluir = correctas.Count > 0,
            Tipo = tipo,
            Enunciado = enunciado,
            Opciones = string.Join("\n", opciones),
            Correctas = string.Join("\n", correctas),
            Retroalimentacion = string.IsNullOrWhiteSpace(explicacion) ? null : explicacion,
            Advertencia = correctas.Count == 0 ? AvisoSinRespuesta : null
        };
    }

    private static string ResolverRespuesta(string valor, IReadOnlyList<string> opciones)
    {
        var limpio = valor.Trim().Trim('.', ')', ' ');
        if (limpio.Length == 1 && char.IsLetter(limpio[0]) && opciones.Count > 0)
        {
            var indice = char.ToUpperInvariant(limpio[0]) - 'A';
            return indice >= 0 && indice < opciones.Count ? opciones[indice] : string.Empty;
        }

        var sinLetra = QuitarLetra(valor);
        return opciones.FirstOrDefault(o => ComparadorRespuestas.SonEquivalentes(o, sinLetra))
            ?? opciones.FirstOrDefault(o => ComparadorRespuestas.Normalizar(sinLetra).Contains(ComparadorRespuestas.Normalizar(o)) && o.Length > 2)
            ?? sinLetra;
    }

    private static string QuitarLetra(string valor) =>
        Regex.Replace(valor.Trim(), @"^[A-F][\)\.]\s+", string.Empty, Opciones).Trim();

    private static bool EsVerdaderoOFalso(string valor)
    {
        var normal = ComparadorRespuestas.Normalizar(valor);
        return normal is "verdadero" or "falso" or "true" or "false" or "v" or "f";
    }

    private static string Compactar(string valor) =>
        Regex.Replace(valor, @"\s+", " ").Trim();

    private static string SinTildes(string valor)
    {
        var descompuesto = valor.Normalize(NormalizationForm.FormD);
        var constructor = new StringBuilder(descompuesto.Length);
        foreach (var caracter in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
                constructor.Append(caracter);
        }

        return constructor.ToString();
    }
}

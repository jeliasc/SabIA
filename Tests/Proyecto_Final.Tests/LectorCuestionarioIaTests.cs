using Proyecto_Final.Models;
using Proyecto_Final.Servicios.GestionCuestionarios;

namespace Proyecto_Final.Tests;

public sealed class LectorCuestionarioIaTests
{
    // Texto real devuelto por la IA (captura del 15/10/2026), en una sola línea.
    private const string TextoCaptura =
        "**Cuestionario de opción múltiple: Fracciones** **Instrucciones:** Selecciona la opción correcta para cada pregunta. --- " +
        "**1. ¿Qué es una fracción?** A) Es una forma de representar números enteros. B) Es una forma de representar una parte de una unidad que está dividida en partes iguales. C) Es una forma de representar números negativos. D) Es una forma de representar números muy grandes. **Respuesta correcta: B** --- " +
        "**2. ¿Cuál es el nombre del número que indica cuántas partes se han tomado de la unidad?** A) Denominador B) Numerador C) Fracción D) Unidad **Respuesta correcta: B** --- " +
        "**3. ¿Cómo se lee el denominador 5 en una fracción?** A) Cinco B) Quintos C) Cincoavos D) Quinto **Respuesta correcta: B** --- " +
        "**4. ¿Qué tipo de fracción es 3/4?** A) Fracción impropia B) Fracción propia C) Fracción unitaria D) Fracción mixta **Respuesta correcta: B** --- " +
        "**5. ¿Qué tipo de fracción es 7/7?** A) Fracción propia B) Fracción impropia C) Fracción unitaria D) Fracción mixta **Respuesta correcta: C** --- " +
        "**Respuestas correctas:** 1. B 2. B 3. B 4. B 5. C";

    [Fact]
    public void TextoDeLaCaptura_SeLeenLasCincoPreguntas()
    {
        var preguntas = LectorCuestionarioIa.Analizar(TextoCaptura);

        Assert.Equal(5, preguntas.Count);
        Assert.All(preguntas, p => Assert.Equal(TipoPregunta.SeleccionUnica, p.Tipo));
        Assert.All(preguntas, p => Assert.Equal(4, p.Opciones!.Split('\n').Length));
        Assert.All(preguntas, p => Assert.Null(p.Advertencia));

        Assert.Equal("¿Qué es una fracción?", preguntas[0].Enunciado);
        Assert.StartsWith("Es una forma de representar una parte", preguntas[0].Correctas);
        Assert.Equal("Numerador", preguntas[1].Correctas);
        Assert.Equal("Quintos", preguntas[2].Correctas);
        Assert.Equal("¿Qué tipo de fracción es 3/4?", preguntas[3].Enunciado);
        Assert.Equal("Fracción propia", preguntas[3].Correctas);
        // La IA marcó C (unitaria) aunque 7/7 es impropia: se respeta lo que dijo
        // la IA y el docente lo corrige en la vista previa.
        Assert.Equal("Fracción unitaria", preguntas[4].Correctas);
    }

    [Fact]
    public void TextoConSaltosDeLinea_YVerdaderoFalsoYRespuestaDirecta()
    {
        const string texto = """
            1. La fotosíntesis ocurre en los cloroplastos.
            Respuesta correcta: Verdadero
            Explicación: Los cloroplastos contienen clorofila.

            2. ¿Cómo se llama el gas que liberan las plantas?
            Respuesta: Oxígeno / O2

            3. Marque los gases nobles:
            a) Helio
            b) Oxígeno
            c) Neón
            Respuestas correctas: A, C
            """;

        var preguntas = LectorCuestionarioIa.Analizar(texto);

        Assert.Equal(3, preguntas.Count);
        Assert.Equal(TipoPregunta.VerdaderoFalso, preguntas[0].Tipo);
        Assert.Equal("Verdadero", preguntas[0].Correctas);
        Assert.Equal("Los cloroplastos contienen clorofila.", preguntas[0].Retroalimentacion);

        Assert.Equal(TipoPregunta.TextoCorto, preguntas[1].Tipo);
        Assert.Equal("Oxígeno\nO2", preguntas[1].Correctas);

        Assert.Equal(TipoPregunta.SeleccionMultiple, preguntas[2].Tipo);
        Assert.Equal("Helio\nNeón", preguntas[2].Correctas);
    }

    [Fact]
    public void Json_ConBloqueDeCodigoYTiposVariados()
    {
        const string texto = """
            Aquí está el cuestionario:
            ```json
            {"preguntas":[
              {"tipo":"seleccion_unica","enunciado":"¿Qué es 1/2?","opciones":["A) Un medio","B) Un tercio"],"correctas":["A"],"explicacion":"1 de 2 partes."},
              {"tipo":"verdadero_falso","enunciado":"3/4 es propia","opciones":["Verdadero","Falso"],"correctas":["Verdadero"]},
              {"tipo":"respuesta_directa","enunciado":"Nombre del número de abajo","correctas":["Denominador"]},
              {"tipo":"seleccion_multiple","enunciado":"Fracciones propias","opciones":["1/2","5/4","2/3"],"correctas":["1/2","2/3"]}
            ]}
            ```
            """;

        var preguntas = LectorCuestionarioIa.Analizar(texto);

        Assert.Equal(4, preguntas.Count);
        Assert.Equal("Un medio\nUn tercio", preguntas[0].Opciones);
        Assert.Equal("Un medio", preguntas[0].Correctas);
        Assert.Equal("1 de 2 partes.", preguntas[0].Retroalimentacion);
        Assert.Equal(TipoPregunta.VerdaderoFalso, preguntas[1].Tipo);
        Assert.Equal(TipoPregunta.TextoCorto, preguntas[2].Tipo);
        Assert.Equal("Denominador", preguntas[2].Correctas);
        Assert.Equal(TipoPregunta.SeleccionMultiple, preguntas[3].Tipo);
        Assert.Equal("1/2\n2/3", preguntas[3].Correctas);
    }

    [Fact]
    public void SinRespuestaIdentificable_QuedaMarcadaYSinIncluir()
    {
        var preguntas = LectorCuestionarioIa.Analizar("1. ¿Cuánto es 2+2? A) 3 B) 4 C) 5");

        var pregunta = Assert.Single(preguntas);
        Assert.False(pregunta.Incluir);
        Assert.Equal(LectorCuestionarioIa.AvisoSinRespuesta, pregunta.Advertencia);
    }

    [Fact]
    public void TextoSinPreguntas_DevuelveListaVacia()
    {
        Assert.Empty(LectorCuestionarioIa.Analizar("Lo siento, no pude generar el cuestionario."));
        Assert.Empty(LectorCuestionarioIa.Analizar(null));
    }

    [Fact]
    public void FormatoParaLaIa_SeAgregaALasInstruccionesDelDocente()
    {
        var instrucciones = FormatoCuestionarioIa.Agregar("Cinco preguntas, nivel básico");

        Assert.StartsWith("Cinco preguntas, nivel básico", instrucciones);
        Assert.Contains("\"preguntas\"", instrucciones);
        Assert.Equal(FormatoCuestionarioIa.Instrucciones, FormatoCuestionarioIa.Agregar("  "));
    }
}

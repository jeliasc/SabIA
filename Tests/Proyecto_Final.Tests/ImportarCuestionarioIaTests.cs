using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.GestionCuestionarios;
using Proyecto_Final.ViewModels.Cuestionarios;
using T = Proyecto_Final.Tests.CuestionarioServicioTests;

namespace Proyecto_Final.Tests;

public sealed class ImportarCuestionarioIaTests
{
    private const string Texto =
        "1. ¿Qué es 1/2? A) Un medio B) Un tercio Respuesta correcta: A " +
        "2. 3/4 es una fracción propia. Respuesta correcta: Verdadero " +
        "3. ¿Cómo se llama el número de abajo? Respuesta correcta: Denominador " +
        "4. ¿Qué tipo de fracción es 7/7? A) Propia B) Impropia C) Unitaria Respuesta correcta: C";

    [Fact]
    public async Task Importar_CreaElCuestionarioYDejaLaTareaEnBorrador()
    {
        await using var contexto = CrearContexto();
        await T.SembrarAsync(contexto, _ => { }, estadoTarea: EstadoTarea.Borrador, sinCuestionario: true);
        var servicio = new CuestionarioServicio(contexto, new T.AccesoPrueba(alumnoId: null, usuario: "docente"));

        var tareas = await servicio.ObtenerTareasParaImportarAsync();
        Assert.Equal(1, Assert.Single(tareas).Id);

        var preguntas = LectorCuestionarioIa.Analizar(Texto);
        Assert.Equal(4, preguntas.Count);

        // El docente corrige la respuesta que la IA dio mal y le da 2 puntos.
        preguntas[3].Correctas = "Impropia";
        preguntas[3].Punteo = 2;

        var resultado = await servicio.ImportarPreguntasAsync(new ImportarCuestionarioIa
        {
            TareaId = 1,
            Preguntas = preguntas
        });

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        contexto.ChangeTracker.Clear();

        var cuestionario = await contexto.Cuestionarios
            .Include(x => x.Tarea)
            .Include(x => x.Preguntas).ThenInclude(x => x.Opciones)
            .Include(x => x.Preguntas).ThenInclude(x => x.RespuestasAceptadas)
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(OrigenCuestionario.InteligenciaArtificial, cuestionario.Origen);
        Assert.Equal(EstadoTarea.Borrador, cuestionario.Tarea.Estado);
        Assert.Equal([1, 2, 3, 4], cuestionario.Preguntas.OrderBy(x => x.Orden).Select(x => x.Orden).ToList());

        var porOrden = cuestionario.Preguntas.OrderBy(x => x.Orden).ToList();
        Assert.Equal("Un medio", porOrden[0].Opciones.Single(x => x.EsCorrecta).Texto);
        Assert.Equal(TipoPregunta.VerdaderoFalso, porOrden[1].Tipo);
        Assert.Equal("Verdadero", porOrden[1].Opciones.Single(x => x.EsCorrecta).Texto);
        Assert.Equal("Denominador", Assert.Single(porOrden[2].RespuestasAceptadas).Texto);
        Assert.Equal("Impropia", porOrden[3].Opciones.Single(x => x.EsCorrecta).Texto);
        Assert.Equal(2, porOrden[3].Punteo);

        // Una segunda importación agrega después de las existentes.
        var segunda = await servicio.ImportarPreguntasAsync(new ImportarCuestionarioIa
        {
            TareaId = 1,
            Preguntas = LectorCuestionarioIa.Analizar("1. ¿Qué es 1/4? A) Un cuarto B) Un quinto Respuesta correcta: A")
        });
        Assert.True(segunda.Exitoso, segunda.Mensaje);
        Assert.Equal(5, await contexto.Preguntas.MaxAsync(x => x.Orden, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Importar_ValidaCadaPreguntaMarcada()
    {
        await using var contexto = CrearContexto();
        await T.SembrarAsync(contexto, _ => { }, estadoTarea: EstadoTarea.Borrador, sinCuestionario: true);
        var servicio = new CuestionarioServicio(contexto, new T.AccesoPrueba(alumnoId: null, usuario: "docente"));

        var preguntas = LectorCuestionarioIa.Analizar(Texto);
        preguntas[0].Correctas = "Un cuarto";      // no coincide con ninguna opción
        preguntas[2].Correctas = "";                // respuesta directa sin respuesta
        preguntas[3].Incluir = false;               // descartada: no se valida

        var resultado = await servicio.ImportarPreguntasAsync(new ImportarCuestionarioIa
        {
            TareaId = 1,
            Preguntas = preguntas
        });

        Assert.False(resultado.Exitoso);
        Assert.Contains("Preguntas[0].Correctas", resultado.Errores.Keys);
        Assert.Contains("Preguntas[2].Correctas", resultado.Errores.Keys);
        Assert.DoesNotContain("Preguntas[3].Correctas", resultado.Errores.Keys);
        Assert.False(await contexto.Preguntas.AnyAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Importar_RechazaTareaPublicadaYAlumno()
    {
        await using var contexto = CrearContexto();
        await T.SembrarAsync(contexto, _ => { });   // tarea publicada con cuestionario
        var docente = new CuestionarioServicio(contexto, new T.AccesoPrueba(alumnoId: null, usuario: "docente"));
        var alumno = new CuestionarioServicio(contexto, new T.AccesoPrueba(alumnoId: 1));
        var modelo = new ImportarCuestionarioIa { TareaId = 1, Preguntas = LectorCuestionarioIa.Analizar(Texto) };

        Assert.False((await docente.ImportarPreguntasAsync(modelo)).Exitoso);
        Assert.Empty(await docente.ObtenerTareasParaImportarAsync());
        Assert.Empty(await alumno.ObtenerTareasParaImportarAsync());
    }

    private static Contexto CrearContexto() => new(
        new DbContextOptionsBuilder<Contexto>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);
}

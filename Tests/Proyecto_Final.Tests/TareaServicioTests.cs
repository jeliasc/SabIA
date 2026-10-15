using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Archivos;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.GestionCuestionarios;
using Proyecto_Final.Servicios.GestionTareas;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Tareas;

namespace Proyecto_Final.Tests;

public sealed class TareaServicioTests
{
    [Fact]
    public async Task CrearCuestionario_CreaBorradorYConfiguracionEnElMismoFlujo()
    {
        await using var contexto = CrearContexto();
        await SembrarEscenarioAsync(contexto);
        var archivos = new ArchivosPrueba();
        var acceso = new AccesoPrueba();
        var servicio = new TareaServicio(contexto, archivos, acceso);

        var resultado = await servicio.CrearAsync(
            Modelo(TipoTarea.Cuestionario),
            TestContext.Current.CancellationToken);

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        Assert.True(resultado.Datos > 0);
        var tarea = await contexto.Tareas
            .Include(x => x.Cuestionario)
            .Include(x => x.Archivos)
            .SingleAsync(x => x.Id == resultado.Datos, TestContext.Current.CancellationToken);
        Assert.Equal(EstadoTarea.Borrador, tarea.Estado);
        Assert.NotNull(tarea.Cuestionario);
        Assert.Empty(tarea.Archivos);
        Assert.Equal(0, archivos.Guardados);

        var configuracion = await new CuestionarioServicio(contexto, acceso)
            .ObtenerConfiguracionAsync(tarea.Id);
        Assert.NotNull(configuracion);
        Assert.Equal(tarea.Id, configuracion!.TareaId);
    }

    [Fact]
    public async Task CrearCuestionario_MismoToken_NoDuplicaTareaNiCuestionario()
    {
        await using var contexto = CrearContexto();
        await SembrarEscenarioAsync(contexto);
        var servicio = new TareaServicio(contexto, new ArchivosPrueba(), new AccesoPrueba());
        var modelo = Modelo(TipoTarea.Cuestionario);

        var primero = await servicio.CrearAsync(modelo, TestContext.Current.CancellationToken);
        var segundo = await servicio.CrearAsync(modelo, TestContext.Current.CancellationToken);

        Assert.True(primero.Exitoso, primero.Mensaje);
        Assert.True(segundo.Exitoso, segundo.Mensaje);
        Assert.Equal(primero.Datos, segundo.Datos);
        Assert.Equal(1, await contexto.Tareas.CountAsync(
            x => x.GrupoVersionId == modelo.TokenCreacion,
            TestContext.Current.CancellationToken));
        Assert.Equal(1, await contexto.Cuestionarios.CountAsync(
            x => x.TareaId == primero.Datos,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CrearCuestionario_ConAdjunto_RechazaSinProcesarArchivo()
    {
        await using var contexto = CrearContexto();
        await SembrarEscenarioAsync(contexto);
        var archivos = new ArchivosPrueba();
        var servicio = new TareaServicio(contexto, archivos, new AccesoPrueba());
        var modelo = Modelo(TipoTarea.Cuestionario);
        modelo.Archivo = ArchivoFormulario();

        var resultado = await servicio.CrearAsync(modelo, TestContext.Current.CancellationToken);

        Assert.False(resultado.Exitoso);
        Assert.Contains(nameof(modelo.Archivo), resultado.Errores.Keys);
        Assert.Equal(0, archivos.Guardados);
        Assert.False(await contexto.Tareas.AnyAsync(
            x => x.GrupoVersionId == modelo.TokenCreacion,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CrearEntregaArchivo_ConservaAdjuntoYBorrador()
    {
        await using var contexto = CrearContexto();
        await SembrarEscenarioAsync(contexto);
        var archivos = new ArchivosPrueba();
        var servicio = new TareaServicio(contexto, archivos, new AccesoPrueba());
        var modelo = Modelo(TipoTarea.Archivo);
        modelo.Archivo = ArchivoFormulario();

        var resultado = await servicio.CrearAsync(modelo, TestContext.Current.CancellationToken);

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        Assert.Equal(1, archivos.Guardados);
        var tarea = await contexto.Tareas
            .Include(x => x.Archivos)
            .SingleAsync(x => x.Id == resultado.Datos, TestContext.Current.CancellationToken);
        Assert.Equal(TipoTarea.Archivo, tarea.Tipo);
        Assert.Equal(EstadoTarea.Borrador, tarea.Estado);
        Assert.Single(tarea.Archivos);
        Assert.Null(await contexto.Cuestionarios.SingleOrDefaultAsync(
            x => x.TareaId == tarea.Id,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PublicarCuestionarioIncompleto_PermaneceEnBorrador()
    {
        await using var contexto = CrearContexto();
        await SembrarEscenarioAsync(contexto);
        var servicio = new TareaServicio(contexto, new ArchivosPrueba(), new AccesoPrueba());
        var creado = await servicio.CrearAsync(
            Modelo(TipoTarea.Cuestionario),
            TestContext.Current.CancellationToken);

        var resultado = await servicio.PublicarAsync(creado.Datos);

        Assert.False(resultado.Exitoso);
        Assert.Contains("configuración", resultado.Mensaje, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(
            EstadoTarea.Borrador,
            (await contexto.Tareas.FindAsync([creado.Datos], TestContext.Current.CancellationToken))!.Estado);
    }

    [Fact]
    public async Task PublicarCuestionarioConfigurado_PermitePublicacion()
    {
        await using var contexto = CrearContexto();
        await SembrarEscenarioAsync(contexto);
        var servicio = new TareaServicio(contexto, new ArchivosPrueba(), new AccesoPrueba());
        var creado = await servicio.CrearAsync(
            Modelo(TipoTarea.Cuestionario),
            TestContext.Current.CancellationToken);
        var cuestionario = await contexto.Cuestionarios.SingleAsync(
            x => x.TareaId == creado.Datos,
            TestContext.Current.CancellationToken);
        cuestionario.Preguntas.Add(new Pregunta
        {
            Orden = 1,
            Tipo = TipoPregunta.SeleccionUnica,
            Enunciado = "Pregunta válida",
            Punteo = 10,
            Opciones =
            [
                new OpcionPregunta { Orden = 1, Texto = "Correcta", EsCorrecta = true },
                new OpcionPregunta { Orden = 2, Texto = "Incorrecta" }
            ]
        });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var resultado = await servicio.PublicarAsync(creado.Datos);

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        Assert.Equal(
            EstadoTarea.Publicada,
            (await contexto.Tareas.FindAsync([creado.Datos], TestContext.Current.CancellationToken))!.Estado);
    }

    [Fact]
    public async Task Crear_AsignacionNoAutorizada_ConservaValidacionExistente()
    {
        await using var contexto = CrearContexto();
        await SembrarEscenarioAsync(contexto);
        var servicio = new TareaServicio(contexto, new ArchivosPrueba(), new AccesoPrueba(false));
        var modelo = Modelo(TipoTarea.Archivo);

        var resultado = await servicio.CrearAsync(modelo, TestContext.Current.CancellationToken);

        Assert.False(resultado.Exitoso);
        Assert.Contains(nameof(modelo.AsignacionId), resultado.Errores.Keys);
    }

    private static CrearTarea Modelo(TipoTarea tipo) => new()
    {
        AsignacionId = 1,
        Tipo = tipo,
        TokenCreacion = Guid.NewGuid(),
        Titulo = tipo == TipoTarea.Cuestionario ? "Cuestionario nuevo" : "Entrega nueva",
        PunteoMaximo = 10,
        FechaDisponibilidad = new DateTime(2026, 10, 15, 8, 0, 0, DateTimeKind.Unspecified),
        FechaLimite = new DateTime(2026, 10, 16, 8, 0, 0, DateTimeKind.Unspecified)
    };

    private static FormFile ArchivoFormulario()
    {
        var contenido = new MemoryStream("contenido"u8.ToArray());
        return new FormFile(contenido, 0, contenido.Length, "Archivo", "guia.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
    }

    private static Contexto CrearContexto() => new(
        new DbContextOptionsBuilder<Contexto>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private static Task SembrarEscenarioAsync(Contexto contexto) =>
        CuestionarioServicioTests.SembrarAsync(
            contexto,
            _ => { },
            EstadoTarea.Borrador,
            sinCuestionario: true);

    private sealed class AccesoPrueba(bool autorizado = true) : IAccesoAcademicoServicio
    {
        public string? UsuarioId => "docente";
        public bool EsOperadorInstitucional => false;
        public bool TienePermiso(string permiso) => true;
        public Task<ContextoAcademicoUsuario> ObtenerContextoUsuarioAsync() =>
            Task.FromResult(new ContextoAcademicoUsuario(1, null));
        public Task<int?> ObtenerDocenteIdAsync() => Task.FromResult<int?>(1);
        public Task<int?> ObtenerAlumnoIdAsync() => Task.FromResult<int?>(null);
        public Task<bool> EsDocenteDeAsignacionAsync(int asignacionId) => Task.FromResult(autorizado);
        public Task<bool> AlumnoPerteneceAsignacionAsync(int asignacionId) => Task.FromResult(false);
        public Task<bool> PuedeConsultarAsignacionVigenteAsync(int asignacionId) => Task.FromResult(autorizado);
        public Task<bool> PuedeConsultarAsignacionHistoricaAsync(int asignacionId) => Task.FromResult(autorizado);
        public Task<bool> PuedeGestionarAsignacionVigenteAsync(int asignacionId) => Task.FromResult(autorizado);
    }

    private sealed class ArchivosPrueba : IArchivoFisicoServicio
    {
        public int Guardados { get; private set; }

        public Task<ResultadoOperacion<ArchivoGuardado>> GuardarAsync(
            IFormFile archivo,
            string categoria,
            CancellationToken cancellationToken = default)
        {
            Guardados++;
            return Task.FromResult(ResultadoOperacion<ArchivoGuardado>.Correcto(new ArchivoGuardado
            {
                NombreOriginal = archivo.FileName,
                ClaveAlmacenamiento = $"pruebas/{Guid.NewGuid():N}",
                TipoMime = archivo.ContentType,
                TamanoBytes = archivo.Length,
                HashSha256 = "hash-prueba"
            }));
        }

        public Task<Stream?> AbrirLecturaAsync(
            string claveAlmacenamiento,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(null);

        public Task EliminarAsync(
            string claveAlmacenamiento,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}

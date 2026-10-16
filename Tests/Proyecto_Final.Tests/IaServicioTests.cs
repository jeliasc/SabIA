using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.Archivos;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.InteligenciaArtificial;
using Proyecto_Final.Servicios.SeguridadAcademica;

namespace Proyecto_Final.Tests;

public sealed class IaServicioTests
{
    [Fact]
    public async Task GenerarDesdeDocumento_EnviaMultipartConInstruccionesYVariosTipos()
    {
        await using var contexto = CrearContexto();
        await SembrarPlanificacionAsync(contexto);
        var handler = new Handler(_ => RespuestaTrabajos("resumen", "cuestionario"));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var servicio = CrearServicio(contexto, handler, cache);
        var resultado = await servicio.GenerarDesdeDocumentoAsync(
            1,
            [
                new SolicitudContenidoIa("resumen", "Use lenguaje sencillo."),
                new SolicitudContenidoIa("cuestionario", "Incluya cinco preguntas.")
            ],
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        Assert.Equal(2, resultado.Datos!.Count);
        Assert.Equal("/api/generar-desde-documento/", handler.Rutas.Single());
        Assert.Equal("clave-prueba", handler.Claves.Single());
        var multipart = handler.Cuerpos.Single();
        Assert.Contains("name=documento", multipart, StringComparison.Ordinal);
        Assert.Contains("name=solicitudes", multipart, StringComparison.Ordinal);
        Assert.Contains("{\"tipo\":\"resumen\",\"instrucciones\":\"Use lenguaje sencillo.\"}", multipart, StringComparison.Ordinal);
        Assert.Contains("{\"tipo\":\"cuestionario\",\"instrucciones\":\"Incluya cinco preguntas.\"}", multipart, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerarDesdeArchivoLocal_NoExigePlanificacionYEnviaElPdf()
    {
        await using var contexto = CrearContexto();
        var handler = new Handler(_ => RespuestaTrabajos("glosario"));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var servicio = CrearServicio(contexto, handler, cache);
        await using var pdf = new MemoryStream(Encoding.UTF8.GetBytes("%PDF-1.7 local"));

        var resultado = await servicio.GenerarDesdeArchivoLocalAsync(
            [new SolicitudContenidoIa("glosario", "Incluya términos clave.")],
            pdf,
            "origen.pdf",
            "application/pdf",
            pdf.Length,
            TestContext.Current.CancellationToken);

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        Assert.Equal("/api/generar-desde-documento/", handler.Rutas.Single());
        Assert.Contains("origen.pdf", handler.Cuerpos.Single(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("origen.txt", "application/pdf", "%PDF-1.7", 8L)]
    [InlineData("origen.pdf", "text/plain", "%PDF-1.7", 8L)]
    [InlineData("origen.pdf", "application/pdf", "texto plano", 11L)]
    [InlineData("origen.pdf", "application/pdf", "%PDF-1.7", 10485761L)]
    public async Task GenerarDesdeArchivoLocal_ValidaExtensionMimeFirmaYTamanio(
        string nombre,
        string mime,
        string contenido,
        long tamanio)
    {
        await using var contexto = CrearContexto();
        var handler = new Handler(_ => throw new InvalidOperationException("No debe invocarse."));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var servicio = CrearServicio(contexto, handler, cache);
        await using var archivo = new MemoryStream(Encoding.UTF8.GetBytes(contenido));

        var resultado = await servicio.GenerarDesdeArchivoLocalAsync(
            [new SolicitudContenidoIa("resumen")],
            archivo,
            nombre,
            mime,
            tamanio,
            TestContext.Current.CancellationToken);

        Assert.False(resultado.Exitoso);
        Assert.Empty(handler.Rutas);
    }

    [Fact]
    public async Task GenerarDirecto_EnviaPromptMaxTokensYEncabezado()
    {
        await using var contexto = CrearContexto();
        var handler = new Handler(_ => RespuestaTrabajos("resumen"));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var servicio = CrearServicio(contexto, handler, cache);

        var resultado = await servicio.GenerarDirectoAsync("Explique fracciones", 650, TestContext.Current.CancellationToken);

        Assert.True(resultado.Exitoso, resultado.Mensaje);
        Assert.Equal("/api/generar/", handler.Rutas.Single());
        Assert.Equal("clave-prueba", handler.Claves.Single());
        Assert.Equal("{\"prompt\":\"Explique fracciones\",\"max_tokens\":650}", handler.Cuerpos.Single());
    }

    [Theory]
    [InlineData("pendiente", null)]
    [InlineData("listo", "contenido listo")]
    [InlineData("error", "falló el procesamiento")]
    public async Task ConsultarEstado_DevuelveEstadosDelServicio(string estado, string? resultadoEsperado)
    {
        await using var contexto = CrearContexto();
        var handler = new Handler(request =>
            request.RequestUri!.AbsolutePath.EndsWith("/generar/", StringComparison.Ordinal)
                ? RespuestaTrabajos("resumen")
                : Task.FromResult(Json($"{{\"estado\":\"{estado}\",\"resultado\":{(resultadoEsperado == null ? "null" : "\"" + resultadoEsperado + "\"")}}}")));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var servicio = CrearServicio(contexto, handler, cache);

        var trabajo = await servicio.GenerarDirectoAsync("estado " + estado, 100, TestContext.Current.CancellationToken);
        var consulta = await servicio.ConsultarEstadoAsync(trabajo.Datos!.Single().JobId, TestContext.Current.CancellationToken);

        Assert.True(consulta.Exitoso, consulta.Mensaje);
        Assert.Equal(estado, consulta.Datos!.Estado);
        Assert.Equal(resultadoEsperado, consulta.Datos.Resultado);
        Assert.Equal($"/api/estado/{trabajo.Datos!.Single().JobId}/", handler.Rutas.Last());
    }

    [Fact]
    public async Task GenerarDirecto_SinClaveNoIntentaConectar()
    {
        await using var contexto = CrearContexto();
        var handler = new Handler(_ => throw new InvalidOperationException("No debe invocarse."));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var servicio = CrearServicio(contexto, handler, cache, apiKey: "");

        var resultado = await servicio.GenerarDirectoAsync("No conectar", 100, TestContext.Current.CancellationToken);

        Assert.False(resultado.Exitoso);
        Assert.Empty(handler.Rutas);
    }

    [Fact]
    public async Task GenerarDirecto_DevuelveErroresHttpYTiempoDeEspera()
    {
        await using var contexto = CrearContexto();
        using var cacheHttp = new MemoryCache(new MemoryCacheOptions());
        var http = CrearServicio(contexto, new Handler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway))), cacheHttp);
        var errorHttp = await http.GenerarDirectoAsync("fallo http", 100, TestContext.Current.CancellationToken);

        using var cacheTiempo = new MemoryCache(new MemoryCacheOptions());
        var tiempo = CrearServicio(contexto, new Handler(_ => throw new TaskCanceledException()), cacheTiempo);
        var errorTiempo = await tiempo.GenerarDirectoAsync("fallo tiempo", 100, TestContext.Current.CancellationToken);

        Assert.False(errorHttp.Exitoso);
        Assert.Contains("502", errorHttp.Mensaje, StringComparison.Ordinal);
        Assert.False(errorTiempo.Exitoso);
        Assert.Contains("tardó demasiado", errorTiempo.Mensaje, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerarDirecto_RespetaCancelacionDelSolicitante()
    {
        await using var contexto = CrearContexto();
        var handler = new Handler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return Json("{}");
        });
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var servicio = CrearServicio(contexto, handler, cache);
        using var cancelacion = new CancellationTokenSource();
        cancelacion.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            servicio.GenerarDirectoAsync("cancelar", 100, cancelacion.Token));
    }

    [Fact]
    public async Task Estado_ImpideAccesoDeOtroUsuarioYDedupeEvitaEnvioDuplicado()
    {
        await using var contexto = CrearContexto();
        var handler = new Handler(_ => RespuestaTrabajos("resumen"));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var propietario = CrearServicio(contexto, handler, cache, usuarioId: "docente-1");

        var primero = await propietario.GenerarDirectoAsync("mismo prompt", 100, TestContext.Current.CancellationToken);
        var repetido = await propietario.GenerarDirectoAsync("mismo prompt", 100, TestContext.Current.CancellationToken);
        var ajeno = CrearServicio(contexto, handler, cache, usuarioId: "docente-2");
        var consultaAjena = await ajeno.ConsultarEstadoAsync(primero.Datos!.Single().JobId, TestContext.Current.CancellationToken);

        Assert.True(primero.Exitoso);
        Assert.True(repetido.Exitoso);
        Assert.Single(handler.Rutas);
        Assert.False(consultaAjena.Exitoso);
        Assert.Single(handler.Rutas);
    }

    private static Contexto CrearContexto() => new(new DbContextOptionsBuilder<Contexto>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private static IaServicio CrearServicio(
        Contexto contexto,
        Handler handler,
        IMemoryCache cache,
        string apiKey = "clave-prueba",
        string usuarioId = "docente-1") =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("http://ia-prueba/api/") },
            Options.Create(new ConfiguracionIa { UrlBase = "http://ia-prueba/api", ApiKey = apiKey }),
            contexto,
            new ArchivosPrueba(),
            new AccesoPrueba(usuarioId),
            cache);

    private static async Task SembrarPlanificacionAsync(Contexto contexto)
    {
        var ciclo = new CicloEscolar { Id = 1, Anio = 2026, Activo = true };
        var grado = new Grado { Id = 1, Nombre = "Primero", Carrera = "General" };
        var seccion = new Seccion { Id = 1, CicloEscolarId = 1, CicloEscolar = ciclo, GradoId = 1, Grado = grado, Nombre = "A" };
        var asignacion = new Asignacion { Id = 1, SeccionId = 1, Seccion = seccion, CursoId = 1, GradoId = 1, DocenteId = 1, Estado = EstadoRegistro.Activo };
        var archivo = new Archivo { Id = 1, NombreOriginal = "plan.pdf", ClaveAlmacenamiento = "plan.pdf", TipoMime = "application/pdf", TamanoBytes = 10, SubidoPorUsuarioId = "docente-1", FechaCreacion = DateTime.UtcNow };
        var planificacion = new Planificacion { Id = 1, DocenteId = 1, Tipo = TipoPlanificacion.Semanal, Titulo = "Planificación", FechaCreacion = DateTime.UtcNow };

        contexto.AddRange(ciclo, grado, seccion, asignacion, archivo, planificacion);
        contexto.PlanificacionesAsignaciones.Add(new PlanificacionAsignacion { Id = 1, PlanificacionId = 1, AsignacionId = 1, FechaInicio = DateOnly.FromDateTime(DateTime.UtcNow), FechaFin = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) });
        contexto.PlanificacionesArchivos.Add(new PlanificacionArchivo { PlanificacionId = 1, ArchivoId = 1 });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static Task<HttpResponseMessage> RespuestaTrabajos(params string[] tipos) =>
        Task.FromResult(Json("{\"trabajos\":[" + string.Join(",", tipos.Select((tipo, indice) => $"{{\"job_id\":\"job-{indice + 1}\",\"tipo\":\"{tipo}\"}}")) + "]}"));

    private static HttpResponseMessage Json(string contenido) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(contenido, Encoding.UTF8, "application/json")
    };

    private sealed class Handler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder;

        public Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) :
            this((request, _) => responder(request))
        {
        }

        public Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
        {
            this.responder = responder;
        }

        public List<string> Rutas { get; } = [];
        public List<string?> Claves { get; } = [];
        public List<string> Cuerpos { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Rutas.Add(request.RequestUri!.AbsolutePath);
            Claves.Add(request.Headers.TryGetValues("X-API-Key", out var valores) ? valores.SingleOrDefault() : null);
            Cuerpos.Add(request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
            return await responder(request, cancellationToken);
        }
    }

    private sealed class ArchivosPrueba : IArchivoFisicoServicio
    {
        public Task<ResultadoOperacion<ArchivoGuardado>> GuardarAsync(IFormFile archivo, string categoria, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Stream?> AbrirLecturaAsync(string claveAlmacenamiento, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(new MemoryStream(Encoding.UTF8.GetBytes("%PDF-1.7 almacenado")));
        public Task EliminarAsync(string claveAlmacenamiento, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class AccesoPrueba(string usuarioId) : IAccesoAcademicoServicio
    {
        public string? UsuarioId => usuarioId;
        public bool EsOperadorInstitucional => false;
        public bool TienePermiso(string permiso) => permiso == Permisos.ContenidoIA.Generar;
        public Task<ContextoAcademicoUsuario> ObtenerContextoUsuarioAsync() => Task.FromResult(new ContextoAcademicoUsuario(1, null));
        public Task<int?> ObtenerDocenteIdAsync() => Task.FromResult<int?>(1);
        public Task<int?> ObtenerAlumnoIdAsync() => Task.FromResult<int?>(null);
        public Task<bool> EsDocenteDeAsignacionAsync(int asignacionId) => Task.FromResult(true);
        public Task<bool> AlumnoPerteneceAsignacionAsync(int asignacionId) => Task.FromResult(false);
        public Task<bool> PuedeConsultarAsignacionVigenteAsync(int asignacionId) => Task.FromResult(true);
        public Task<bool> PuedeConsultarAsignacionHistoricaAsync(int asignacionId) => Task.FromResult(false);
        public Task<bool> PuedeGestionarAsignacionVigenteAsync(int asignacionId) => Task.FromResult(true);
    }
}

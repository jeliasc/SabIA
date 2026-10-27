using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Proyecto_Final.Controllers;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.GestionEntregas;
using Proyecto_Final.ViewModels.Entregas;

namespace Proyecto_Final.Tests;

public sealed class EntregasControllerTests
{
    [Fact]
    public void CalificarCero_ConfirmacionMarcada_NoEsRechazadaPorLaValidacionDelModelo()
    {
        var modelo = new CalificarCeroEntrega { Id = 18, Confirmado = true };
        var resultados = new List<ValidationResult>();

        var valido = Validator.TryValidateObject(modelo,
            new ValidationContext(modelo), resultados, validateAllProperties: true);

        Assert.True(valido);
        Assert.Empty(resultados);
    }

    [Fact]
    public async Task Calificar_ModeloInvalido_ConservaElIntentoYRehidrataElSelector()
    {
        var servicio = new EntregaServicioPrueba();
        var controlador = new EntregasController(servicio, null!, null!);
        var enviado = new CalificarEntrega
        {
            Id = 18,
            Calificacion = 7,
            Retroalimentacion = "Observación del docente.",
            Devolver = true
        };
        controlador.ModelState.AddModelError(nameof(enviado.Calificacion), "Valor inválido.");

        var resultado = await controlador.Calificar(enviado);

        var vista = Assert.IsType<ViewResult>(resultado);
        var modelo = Assert.IsType<CalificarEntrega>(vista.Model);
        Assert.Same(enviado, modelo);
        Assert.Equal(18, modelo.Id);
        Assert.Equal(7, modelo.Calificacion);
        Assert.Equal("Observación del docente.", modelo.Retroalimentacion);
        Assert.True(modelo.Devolver);
        Assert.Equal("Alumno de prueba", modelo.Alumno);
        Assert.Equal("Actividad de prueba", modelo.Tarea);
        Assert.Collection(modelo.Intentos,
            intento => Assert.Equal(18, intento.Id),
            intento => Assert.Equal(16, intento.Id));
        Assert.Equal([18], servicio.SolicitudesCalificar);
    }

    [Fact]
    public async Task Calificar_ErrorDelServicioEnCalificacion_ConservaElPostYLoExponeEnElCampo()
    {
        var servicio = new EntregaServicioPrueba
        {
            ResultadoCalificar = ResultadoOperacion.Validacion(
                nameof(CalificarEntrega.Calificacion),
                "La calificación debe estar entre 0 y 100.00.")
        };
        var controlador = new EntregasController(servicio, null!, null!);
        var enviado = new CalificarEntrega
        {
            Id = 18,
            Calificacion = 101,
            Retroalimentacion = "Conservar comentario.",
            Devolver = true
        };

        var resultado = await controlador.Calificar(enviado);

        var vista = Assert.IsType<ViewResult>(resultado);
        var modelo = Assert.IsType<CalificarEntrega>(vista.Model);
        Assert.Same(enviado, modelo);
        Assert.Equal(101, modelo.Calificacion);
        Assert.Equal("Conservar comentario.", modelo.Retroalimentacion);
        Assert.True(modelo.Devolver);
        Assert.Equal("La calificación debe estar entre 0 y 100.00.",
            controlador.ModelState[nameof(CalificarEntrega.Calificacion)]!.Errors.Single().ErrorMessage);
        Assert.Equal([18], servicio.SolicitudesCalificar);
    }

    private sealed class EntregaServicioPrueba : IEntregaServicio
    {
        public List<int> SolicitudesCalificar { get; } = [];
        public ResultadoOperacion ResultadoCalificar { get; init; } =
            ResultadoOperacion.Error("No usado.");

        public Task<List<EntregaLista>> ObtenerTodasAsync() =>
            Task.FromResult(new List<EntregaLista>());

        public Task<List<EntregaLista>> ObtenerHistorialAsync() =>
            Task.FromResult(new List<EntregaLista>());

        public Task<EntregarTarea?> ObtenerParaEntregarAsync(int id) =>
            Task.FromResult<EntregarTarea?>(null);

        public Task<DetalleEntrega?> ObtenerDetalleAsync(int id) =>
            Task.FromResult<DetalleEntrega?>(null);

        public Task<ResultadoOperacion> EntregarAsync(EntregarTarea m, CancellationToken ct) =>
            Task.FromResult(ResultadoOperacion.Error("No usado."));

        public Task<CalificarEntrega?> ObtenerParaCalificarAsync(int id)
        {
            SolicitudesCalificar.Add(id);
            return Task.FromResult<CalificarEntrega?>(new CalificarEntrega
            {
                Id = id,
                Alumno = "Alumno de prueba",
                Tarea = "Actividad de prueba",
                PunteoMaximo = 10,
                TareaId = 4,
                AlumnoId = 2,
                InscripcionId = 3,
                Intentos =
                [
                    new IntentoSeleccionCalificar { Id = 18, NumeroEnvio = 2 },
                    new IntentoSeleccionCalificar { Id = 16, NumeroEnvio = 1 }
                ]
            });
        }

        public Task<ResultadoOperacion> CalificarAsync(CalificarEntrega m) =>
            Task.FromResult(ResultadoCalificar);

        public Task<CalificarCeroEntrega?> ObtenerParaCalificarCeroAsync(int id) =>
            Task.FromResult<CalificarCeroEntrega?>(null);

        public Task<ResultadoOperacion> CalificarCeroAsync(CalificarCeroEntrega m) =>
            Task.FromResult(ResultadoOperacion.Error("No usado."));

        public Task<RectificarCeroAdministrativo?> ObtenerParaRectificarCeroAsync(int id) =>
            Task.FromResult<RectificarCeroAdministrativo?>(null);

        public Task<ResultadoOperacion> RectificarCeroAsync(RectificarCeroAdministrativo m) =>
            Task.FromResult(ResultadoOperacion.Error("No usado."));

        public Task<ReabrirEntrega?> ObtenerParaReabrirAsync(int id) =>
            Task.FromResult<ReabrirEntrega?>(null);

        public Task<ResultadoOperacion> ReabrirAsync(ReabrirEntrega m) =>
            Task.FromResult(ResultadoOperacion.Error("No usado."));

        public Task<(Stream Stream, string Mime, string Nombre)?> DescargarArchivoAsync(int archivoId) =>
            Task.FromResult<(Stream Stream, string Mime, string Nombre)?>(null);

        public Task<(Stream Stream, string Mime, string Nombre)?> VisualizarArchivoAsync(int archivoId) =>
            Task.FromResult<(Stream Stream, string Mime, string Nombre)?>(null);

        public Task<(Stream Stream, string Mime, string Nombre)?> DescargarArchivoTareaAsync(int archivoId) =>
            Task.FromResult<(Stream Stream, string Mime, string Nombre)?>(null);
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Proyecto_Final.Controllers;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.GestionResolucionesTraslados;
using Proyecto_Final.ViewModels.ResolucionesTraslados;

namespace Proyecto_Final.Tests;

public sealed class ResolucionesTrasladosControllerTests
{
    [Fact]
    public async Task Detalle_NoDisponible_ConservaNotFound()
    {
        var controlador = CrearControlador(new ResolucionServicioPrueba());

        var resultado = await controlador.Detalle(18);

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public async Task Detalle_FueraDeCicloAutorizado_RedirigeAlIndiceConMensajeInstitucional()
    {
        var controlador = CrearControlador(new ResolucionServicioPrueba
        {
            ResultadoDetalle = ResultadoConsultaDetalleResolucionTraslado.FueraDeCicloAutorizado()
        });

        var resultado = await controlador.Detalle(18);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(ResolucionesTrasladosController.Index), redireccion.ActionName);
        Assert.Null(redireccion.ControllerName);
        Assert.Equal(
            "La resoluci\u00F3n solicitada pertenece a otro ciclo escolar. Seleccione ese ciclo para consultarla.",
            controlador.TempData["Error"]);
    }

    [Fact]
    public async Task Detalle_Disponible_MuestraLaVista()
    {
        var detalle = new DetalleResolucionTraslado { Id = 18 };
        var controlador = CrearControlador(new ResolucionServicioPrueba
        {
            ResultadoDetalle = ResultadoConsultaDetalleResolucionTraslado.Disponible(detalle)
        });

        var resultado = await controlador.Detalle(18);

        Assert.Same(detalle, Assert.IsType<ViewResult>(resultado).Model);
    }

    private static ResolucionesTrasladosController CrearControlador(
        IResolucionTrasladoAcademicoServicio servicio)
    {
        var http = new DefaultHttpContext();
        return new ResolucionesTrasladosController(servicio, null!)
        {
            ControllerContext = new ControllerContext { HttpContext = http },
            TempData = new TempDataDictionary(http, new TempDataPrueba())
        };
    }

    private sealed class ResolucionServicioPrueba : IResolucionTrasladoAcademicoServicio
    {
        public ResultadoConsultaDetalleResolucionTraslado ResultadoDetalle { get; init; } =
            ResultadoConsultaDetalleResolucionTraslado.NoDisponible();

        public Task<ResolucionesTrasladosIndex> ObtenerIndexAsync(bool puedeGestionar, bool puedeAprobar) =>
            Task.FromResult(new ResolucionesTrasladosIndex());

        public Task<CrearResolucionTraslado?> PrepararCreacionAsync(
            int inscripcionId, int cursoId, int periodoId) =>
            Task.FromResult<CrearResolucionTraslado?>(null);

        public Task<ResultadoOperacion<int>> CrearAsync(CrearResolucionTraslado modelo) =>
            Task.FromResult(ResultadoOperacion<int>.Error("No usado."));

        public Task<DetalleResolucionTraslado?> ObtenerDetalleAsync(int id) =>
            Task.FromResult<DetalleResolucionTraslado?>(null);

        public Task<ResultadoConsultaDetalleResolucionTraslado> ObtenerDetalleParaNavegacionAsync(int id) =>
            Task.FromResult(ResultadoDetalle);

        public Task<ResultadoOperacion> DecidirAsync(DecidirResolucionTraslado modelo) =>
            Task.FromResult(ResultadoOperacion.Error("No usado."));
    }

    private sealed class TempDataPrueba : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) =>
            new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}

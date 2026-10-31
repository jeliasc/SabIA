using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Proyecto_Final.Controllers;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.GestionCiclos;

namespace Proyecto_Final.Tests;

public sealed class ContextoCicloNavegacionTests
{
    [Fact]
    public async Task Seleccionar_DesdeLibroDeOtroCiclo_RegresaAlIndiceDeCalificaciones()
    {
        var controlador = CrearControlador(new ContextoCicloPrueba(2026));

        var resultado = await controlador.Seleccionar(2025, "/Calificaciones/Libro/3");

        Assert.Equal("/Calificaciones", Assert.IsType<LocalRedirectResult>(resultado).Url);
    }

    [Fact]
    public async Task Seleccionar_MismoCiclo_ConservaLaRutaActual()
    {
        var controlador = CrearControlador(new ContextoCicloPrueba(2026));

        var resultado = await controlador.Seleccionar(2026, "/Calificaciones/Libro/3");

        Assert.Equal("/Calificaciones/Libro/3", Assert.IsType<LocalRedirectResult>(resultado).Url);
    }

    [Fact]
    public async Task Seleccionar_RutaDesconocida_UsaInicioComoDestinoSeguro()
    {
        var controlador = CrearControlador(new ContextoCicloPrueba(2026));

        var resultado = await controlador.Seleccionar(2025, "/Ruta/Inexistente/3");

        Assert.Equal("/Home", Assert.IsType<LocalRedirectResult>(resultado).Url);
    }

    private static ContextoCicloController CrearControlador(IContextoCicloEscolar contexto) => new(contexto)
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        TempData = new TempDataDictionary(new DefaultHttpContext(), new TempDataPrueba()),
        Url = new UrlPrueba()
    };

    private sealed class ContextoCicloPrueba(int anio) : IContextoCicloEscolar
    {
        public Task<ContextoCicloEscolar> ObtenerAsync() => Task.FromResult(new ContextoCicloEscolar
        {
            CicloEscolarId = anio
        });

        public Task<ResultadoOperacion> SeleccionarAsync(int cicloEscolarId) =>
            Task.FromResult(ResultadoOperacion.Correcto("Actualizado"));

        public void Restablecer()
        {
        }
    }

    private sealed class TempDataPrueba : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }

    private sealed class UrlPrueba : IUrlHelper
    {
        public ActionContext ActionContext => new();
        public string? Action(UrlActionContext actionContext) => null;
        public string? Content(string? contentPath) => contentPath;
        public bool IsLocalUrl(string? url) => !string.IsNullOrWhiteSpace(url) &&
            url.StartsWith('/') && !url.StartsWith("//");
        public string? Link(string? routeName, object? values) => null;
        public string? RouteUrl(UrlRouteContext routeContext) => null;
    }
}

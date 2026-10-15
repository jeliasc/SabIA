using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Proyecto_Final.Controllers;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.GestionTareas;
using Proyecto_Final.ViewModels.Tareas;

namespace Proyecto_Final.Tests;

public sealed class TareasControllerTests
{
    [Fact]
    public async Task CrearCuestionario_Valido_RedirigeAConfiguracionDeLaMismaTarea()
    {
        var servicio = new TareaServicioPrueba(42);
        var controlador = CrearControlador(servicio, autorizadoCuestionarios: true);

        var resultado = await controlador.Crear(
            ModeloCuestionario(),
            TestContext.Current.CancellationToken);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(CuestionariosController.Configurar), redireccion.ActionName);
        Assert.Equal("Cuestionarios", redireccion.ControllerName);
        Assert.Equal(42, redireccion.RouteValues!["tareaId"]);
        Assert.Equal(1, servicio.Creaciones);
    }

    [Fact]
    public async Task CrearCuestionario_SinPermiso_NoInvocaServicioNiCreaRegistros()
    {
        var servicio = new TareaServicioPrueba(42);
        var controlador = CrearControlador(servicio, autorizadoCuestionarios: false);

        var resultado = await controlador.Crear(
            ModeloCuestionario(),
            TestContext.Current.CancellationToken);

        Assert.IsType<ForbidResult>(resultado);
        Assert.Equal(0, servicio.Creaciones);
    }

    [Fact]
    public async Task CrearCuestionario_FormularioInvalido_PermaneceEnLaVista()
    {
        var servicio = new TareaServicioPrueba(42);
        var controlador = CrearControlador(servicio, autorizadoCuestionarios: true);
        var modelo = ModeloCuestionario();
        controlador.ModelState.AddModelError(nameof(modelo.Titulo), "El título es obligatorio.");

        var resultado = await controlador.Crear(
            modelo,
            TestContext.Current.CancellationToken);

        var vista = Assert.IsType<ViewResult>(resultado);
        Assert.Same(modelo, vista.Model);
        Assert.False(controlador.ModelState.IsValid);
        Assert.Equal(0, servicio.Creaciones);
    }

    private static CrearTarea ModeloCuestionario() => new()
    {
        AsignacionId = 1,
        Tipo = TipoTarea.Cuestionario,
        TokenCreacion = Guid.NewGuid(),
        Titulo = "Evaluación",
        PunteoMaximo = 10
    };

    private static TareasController CrearControlador(
        ITareaServicio servicio,
        bool autorizadoCuestionarios)
    {
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "docente")],
                "Pruebas"))
        };
        var controlador = new TareasController(
            servicio,
            new AutorizacionPrueba(autorizadoCuestionarios))
        {
            ControllerContext = new ControllerContext { HttpContext = http },
            TempData = new TempDataDictionary(http, new TempDataPrueba())
        };
        return controlador;
    }

    private sealed class TareaServicioPrueba(int tareaId) : ITareaServicio
    {
        public int Creaciones { get; private set; }

        public Task<List<TareaLista>> ObtenerTodosAsync() => Task.FromResult(new List<TareaLista>());
        public Task<List<TareaLista>> ObtenerHistorialAsync() => Task.FromResult(new List<TareaLista>());
        public Task PrepararAsync(FormularioTarea m) => Task.CompletedTask;

        public Task<ResultadoOperacion<int>> CrearAsync(CrearTarea m, CancellationToken ct)
        {
            Creaciones++;
            return Task.FromResult(ResultadoOperacion<int>.Correcto(
                tareaId,
                "Tarea creada."));
        }

        public Task<EditarTarea?> ObtenerEditarAsync(int id) => Task.FromResult<EditarTarea?>(null);
        public Task<ResultadoOperacion> EditarAsync(EditarTarea m, CancellationToken ct) =>
            Task.FromResult(ResultadoOperacion.Error("No usado."));
        public Task<ResultadoOperacion> PublicarAsync(int id) =>
            Task.FromResult(ResultadoOperacion.Error("No usado."));
        public Task<ResultadoOperacion> CerrarAsync(int id) =>
            Task.FromResult(ResultadoOperacion.Error("No usado."));
    }

    private sealed class AutorizacionPrueba(bool autorizado) : IAuthorizationService
    {
        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            object? resource,
            IEnumerable<IAuthorizationRequirement> requirements) =>
            Task.FromResult(autorizado
                ? AuthorizationResult.Success()
                : AuthorizationResult.Failed());

        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            object? resource,
            string policyName) =>
            Task.FromResult(autorizado
                ? AuthorizationResult.Success()
                : AuthorizationResult.Failed());
    }

    private sealed class TempDataPrueba : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) =>
            new Dictionary<string, object>();

        public void SaveTempData(
            HttpContext context,
            IDictionary<string, object> values)
        {
        }
    }
}

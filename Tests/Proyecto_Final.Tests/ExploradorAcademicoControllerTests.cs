using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Controllers;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.ExploradorAcademico;
using Proyecto_Final.ViewModels.ExploradorAcademico;

namespace Proyecto_Final.Tests;

public sealed class ExploradorAcademicoControllerTests
{
    [Fact]
    public async Task Seccion_InexistenteYAjena_DevuelvenElMismo404()
    {
        var servicio = new ExploradorServicioPrueba();
        var controlador = CrearControlador(servicio);

        var inexistente = await controlador.Seccion(999);
        var ajena = await controlador.Seccion(200);

        Assert.IsType<NotFoundResult>(inexistente);
        Assert.IsType<NotFoundResult>(ajena);
        Assert.Equal([999, 200], servicio.SeccionesSolicitadas);
    }

    [Fact]
    public async Task Curso_InexistenteYAjeno_DevuelvenElMismo404()
    {
        var servicio = new ExploradorServicioPrueba();
        var controlador = CrearControlador(servicio);

        var inexistente = await controlador.Curso(999);
        var ajeno = await controlador.Curso(200);

        Assert.IsType<NotFoundResult>(inexistente);
        Assert.IsType<NotFoundResult>(ajeno);
        Assert.Equal([999, 200], servicio.CursosSolicitados);
    }

    [Fact]
    public async Task Seccion_AutorizadaEnElCicloSeleccionado_MuestraLaVista()
    {
        var esperado = new ExploradorSeccionAcademico { SeccionId = 10, Seccion = "A" };
        var servicio = new ExploradorServicioPrueba { SeccionAutorizada = esperado };
        var controlador = CrearControlador(servicio);

        var resultado = await controlador.Seccion(10);

        Assert.Same(esperado, Assert.IsType<ViewResult>(resultado).Model);
    }

    [Fact]
    public async Task Curso_AutorizadoEnElCicloSeleccionado_MuestraLaVista()
    {
        var esperado = new ExploradorCursoAcademico { AsignacionId = 10, Curso = "Matemática" };
        var servicio = new ExploradorServicioPrueba { CursoAutorizado = esperado };
        var controlador = CrearControlador(servicio);

        var resultado = await controlador.Curso(10);

        Assert.Same(esperado, Assert.IsType<ViewResult>(resultado).Model);
    }

    [Fact]
    public async Task RecursoDelAmbitoAutorizadoPeroDeOtroCiclo_SinClasificacionSegura_Devuelve404()
    {
        var servicio = new ExploradorServicioPrueba();
        var controlador = CrearControlador(servicio);

        var seccion = await controlador.Seccion(300);
        var curso = await controlador.Curso(300);

        Assert.IsType<NotFoundResult>(seccion);
        Assert.IsType<NotFoundResult>(curso);
    }

    [Fact]
    public void UsuarioSinPermisos_NoSatisfaceLasPoliticasExigidasPorElExplorador()
    {
        var usuario = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "sin-permisos")], "Pruebas"));
        var politicas = typeof(ExploradorAcademicoController)
            .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Select(x => x.Policy)
            .ToArray();

        Assert.False(AutorizacionDashboardCoordinacion.PuedeVer(usuario));
        Assert.False(usuario.HasClaim(TiposClaims.Permiso, Permisos.Secciones.Ver));
        Assert.Contains(Permisos.Dashboard.CoordinacionVer, politicas);
        Assert.Contains(Permisos.Secciones.Ver, politicas);
    }

    private static ExploradorAcademicoController CrearControlador(
        IExploradorAcademicoServicio servicio)
    {
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "coordinacion-prueba")], "Pruebas"))
        };
        return new ExploradorAcademicoController(servicio)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
    }

    private sealed class ExploradorServicioPrueba : IExploradorAcademicoServicio
    {
        public List<int> SeccionesSolicitadas { get; } = [];
        public List<int> CursosSolicitados { get; } = [];
        public ExploradorSeccionAcademico? SeccionAutorizada { get; init; }
        public ExploradorCursoAcademico? CursoAutorizado { get; init; }

        public Task<ExploradorAcademicoIndice?> ObtenerIndiceAsync() =>
            Task.FromResult<ExploradorAcademicoIndice?>(null);

        public Task<ExploradorGradoAcademico?> ObtenerGradoAsync(int gradoId) =>
            Task.FromResult<ExploradorGradoAcademico?>(null);

        public Task<ExploradorSeccionAcademico?> ObtenerSeccionAsync(
            int seccionId,
            ClaimsPrincipal usuario)
        {
            SeccionesSolicitadas.Add(seccionId);
            return Task.FromResult(seccionId == 10 ? SeccionAutorizada : null);
        }

        public Task<ExploradorCursoAcademico?> ObtenerCursoAsync(
            int asignacionId,
            ClaimsPrincipal usuario)
        {
            CursosSolicitados.Add(asignacionId);
            return Task.FromResult(asignacionId == 10 ? CursoAutorizado : null);
        }
    }
}

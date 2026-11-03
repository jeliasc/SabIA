using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Seguridad;

namespace Proyecto_Final.Tests;

[Collection("PostgreSQL temporal")]
public sealed class AlumnoHttpTests : IClassFixture<AlumnoHttpTests.Fixture>
{
    private const string ConfirmacionEsperada = "BASE_DESCARTABLE";
    private const string UsuarioAlumnoId = "alumno-http-prueba";
    private const string UsuarioDocenteDelegadoId = "docente-delegado-http";
    private const string UsuarioDocenteSinDelegacionId = "docente-ajeno-alumno-http";
    private const string UsuarioOperadorCalificacionesId = "operador-calificaciones-http";
    private const string UsuarioSoloVerCalificacionesId = "solo-ver-calificaciones-http";
    private const string UsuarioSinPermisoId = "sin-permiso-http-prueba";
    private readonly Fixture fixture;
    private EscenarioAlumno escenario => fixture.Escenario;

    public AlumnoHttpTests(Fixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task Alumno_ConRolSembrado_VisualizaSoloSuActividadVigente()
    {
        OmitirSinPostgresDescartable();
        using var cliente = CrearCliente();

        var dashboard = await cliente.GetAsync("/?contexto=alumno", TestContext.Current.CancellationToken);
        var perfil = await cliente.GetAsync("/Alumnos/Perfil", TestContext.Current.CancellationToken);
        var propio = await cliente.GetAsync(
            $"/Alumnos/Ver?id={escenario.AlumnoPropioId}", TestContext.Current.CancellationToken);
        var unidades = await cliente.GetAsync("/Unidades", TestContext.Current.CancellationToken);
        var tareas = await cliente.GetAsync("/Tareas", TestContext.Current.CancellationToken);
        var entregas = await cliente.GetAsync("/Entregas", TestContext.Current.CancellationToken);
        var entregar = await cliente.GetAsync(
            $"/Entregas/Entregar?id={escenario.EntregaPendienteId}", TestContext.Current.CancellationToken);
        var detalleEntrega = await cliente.GetAsync(
            $"/Entregas/Detalle?id={escenario.EntregaPendienteId}", TestContext.Current.CancellationToken);
        var cuestionario = await cliente.GetAsync(
            $"/Cuestionarios/Resolver?id={escenario.CuestionarioPropioId}", TestContext.Current.CancellationToken);
        var calificaciones = await cliente.GetAsync("/Calificaciones", TestContext.Current.CancellationToken);
        var notificaciones = await cliente.GetAsync("/Notificaciones", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
        var htmlDashboard = await dashboard.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("Mi espacio acad", htmlDashboard);
        Assert.Contains("Curso alumno HTTP", htmlDashboard);
        Assert.DoesNotContain("Curso ajeno alumno HTTP", htmlDashboard);
        Assert.Equal(HttpStatusCode.OK, perfil.StatusCode);
        Assert.Equal(HttpStatusCode.OK, propio.StatusCode);
        Assert.Equal(HttpStatusCode.OK, unidades.StatusCode);
        Assert.Equal(HttpStatusCode.OK, tareas.StatusCode);
        Assert.Equal(HttpStatusCode.OK, entregas.StatusCode);
        Assert.Equal(HttpStatusCode.OK, entregar.StatusCode);
        Assert.Equal(HttpStatusCode.OK, detalleEntrega.StatusCode);
        Assert.Equal(HttpStatusCode.OK, cuestionario.StatusCode);
        Assert.Equal(HttpStatusCode.OK, calificaciones.StatusCode);
        Assert.Equal(HttpStatusCode.OK, notificaciones.StatusCode);
    }

    [Fact]
    public async Task Alumno_NoAccedeARecursosAjenosNiOperacionesDocentes_YLosPostExigenAntiforgery()
    {
        OmitirSinPostgresDescartable();
        using var cliente = CrearCliente(sinRedireccion: true);

        var alumnoAjeno = await cliente.GetAsync(
            $"/Alumnos/Ver?id={escenario.AlumnoAjenoId}", TestContext.Current.CancellationToken);
        var entregaAjena = await cliente.GetAsync(
            $"/Entregas/Detalle?id={escenario.EntregaAjenaId}", TestContext.Current.CancellationToken);
        var cuestionarioAjeno = await cliente.GetAsync(
            $"/Cuestionarios/Resolver?id={escenario.CuestionarioAjenoId}", TestContext.Current.CancellationToken);
        var crearTarea = await cliente.GetAsync("/Tareas/Crear", TestContext.Current.CancellationToken);
        var crearUnidad = await cliente.GetAsync("/Unidades/Crear", TestContext.Current.CancellationToken);
        var crearPlanificacion = await cliente.GetAsync("/Planificaciones/Crear", TestContext.Current.CancellationToken);
        var configurarCalificaciones = await cliente.GetAsync(
            $"/Calificaciones/Configurar?asignacionId={escenario.AsignacionPropiaId}&periodoId={escenario.PeriodoId}",
            TestContext.Current.CancellationToken);
        var iniciarSinAntiforgery = await cliente.PostAsync(
            $"/Cuestionarios/Iniciar?id={escenario.CuestionarioPropioId}",
            content: null,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, alumnoAjeno.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, entregaAjena.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, cuestionarioAjeno.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, crearTarea.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, crearUnidad.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, crearPlanificacion.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, configurarCalificaciones.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, iniciarSinAntiforgery.StatusCode);
    }

    [Fact]
    public async Task Alumno_CalificacionesNoExponeFilasDeCompanerosNiConRutasManipuladas()
    {
        OmitirSinPostgresDescartable();
        using var cliente = CrearCliente(sinRedireccion: true);

        var control = await cliente.GetAsync(
            $"/Calificaciones/ControlTareas?asignacionId={escenario.AsignacionPropiaId}",
            TestContext.Current.CancellationToken);
        var abacus = await cliente.GetAsync(
            $"/Calificaciones/Abacus?id={escenario.ConfiguracionCalificacionesId}",
            TestContext.Current.CancellationToken);
        var controlAjeno = await cliente.GetAsync(
            $"/Calificaciones/ControlTareas?asignacionId={escenario.AsignacionAjenaId}",
            TestContext.Current.CancellationToken);
        var abacusInexistente = await cliente.GetAsync(
            "/Calificaciones/Abacus?id=2147483647", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, control.StatusCode);
        var htmlControl = await control.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("Alumno HTTP", htmlControl);
        Assert.DoesNotContain("Compañero Expuesto", htmlControl);

        Assert.Equal(HttpStatusCode.OK, abacus.StatusCode);
        var htmlAbacus = await abacus.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("Alumno HTTP", htmlAbacus);
        Assert.DoesNotContain("Compañero Expuesto", htmlAbacus);

        Assert.Equal(HttpStatusCode.NotFound, controlAjeno.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, abacusInexistente.StatusCode);
    }

    [Fact]
    public async Task Libro_CalificaNavegacionSeguraSegunAutorizacionYCiclo()
    {
        OmitirSinPostgresDescartable();
        using var alumno = CrearCliente(sinRedireccion: true);
        using var fabricaDocente = new FabricaSabiaAlumno(
            fixture.ConexionAplicacion!, fixture.DirectorioClaves!, "docente-alumno-http");
        using var docente = fabricaDocente.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        using var fabricaDelegada = new FabricaSabiaAlumno(
            fixture.ConexionAplicacion!, fixture.DirectorioClaves!, UsuarioDocenteDelegadoId);
        using var delegada = fabricaDelegada.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        using var fabricaSinDelegacion = new FabricaSabiaAlumno(
            fixture.ConexionAplicacion!, fixture.DirectorioClaves!, UsuarioDocenteSinDelegacionId);
        using var sinDelegacion = fabricaSinDelegacion.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        using var fabricaSinPermiso = new FabricaSabiaAlumno(
            fixture.ConexionAplicacion!, fixture.DirectorioClaves!, UsuarioSinPermisoId);
        using var sinPermiso = fabricaSinPermiso.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var libroPropio = await alumno.GetAsync(
            $"/Calificaciones/Libro?id={escenario.ConfiguracionCalificacionesId}",
            TestContext.Current.CancellationToken);
        var libroHistorico = await alumno.GetAsync(
            $"/Calificaciones/Libro?id={escenario.ConfiguracionCalificacionesHistoricaId}",
            TestContext.Current.CancellationToken);
        var inexistente = await alumno.GetAsync(
            "/Calificaciones/Libro?id=2147483647", TestContext.Current.CancellationToken);
        var ajeno = await alumno.GetAsync(
            $"/Calificaciones/Libro?id={escenario.ConfiguracionCalificacionesAjenaId}",
            TestContext.Current.CancellationToken);
        var libroDocente = await docente.GetAsync(
            $"/Calificaciones/Libro?id={escenario.ConfiguracionCalificacionesId}",
            TestContext.Current.CancellationToken);
        var libroDelegada = await delegada.GetAsync(
            $"/Calificaciones/Libro?id={escenario.ConfiguracionCalificacionesId}",
            TestContext.Current.CancellationToken);
        var libroSinDelegacion = await sinDelegacion.GetAsync(
            $"/Calificaciones/Libro?id={escenario.ConfiguracionCalificacionesId}",
            TestContext.Current.CancellationToken);
        var libroSinPermiso = await sinPermiso.GetAsync(
            $"/Calificaciones/Libro?id={escenario.ConfiguracionCalificacionesId}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, libroPropio.StatusCode);
        var contenidoLibroPropio = await libroPropio.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);
        Assert.Contains("Alumno HTTP", contenidoLibroPropio);
        Assert.DoesNotContain("ALU-HTTP-3", contenidoLibroPropio);

        Assert.Equal(HttpStatusCode.Found, libroHistorico.StatusCode);
        Assert.Equal("/Calificaciones", libroHistorico.Headers.Location?.OriginalString);

        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, ajeno.StatusCode);
        Assert.Null(inexistente.Headers.Location);
        Assert.Null(ajeno.Headers.Location);
        var contenidoNoDisponible = await inexistente.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);
        Assert.Equal(contenidoNoDisponible, await ajeno.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken));
        Assert.DoesNotContain("Curso ajeno alumno HTTP", contenidoNoDisponible);

        Assert.Equal(HttpStatusCode.OK, libroDocente.StatusCode);
        Assert.Equal(HttpStatusCode.OK, libroDelegada.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, libroSinDelegacion.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, libroSinPermiso.StatusCode);

        var indice = await alumno.GetAsync("/Calificaciones", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, indice.StatusCode);
        var contenidoIndice = await indice.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains(
            "El libro solicitado pertenece a otro ciclo escolar. Seleccione ese ciclo para consultarlo.",
            contenidoIndice);
        Assert.Contains("Curso alumno HTTP", contenidoIndice);
        Assert.DoesNotContain("Curso historico alumno HTTP", contenidoIndice);

        var segundaConsultaHistorica = await alumno.GetAsync(
            $"/Calificaciones/Libro?id={escenario.ConfiguracionCalificacionesHistoricaId}",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Found, segundaConsultaHistorica.StatusCode);
        Assert.Equal("/Calificaciones", segundaConsultaHistorica.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Abacus_CalificaNavegacionSeguraSegunAutorizacionYCiclo()
    {
        OmitirSinPostgresDescartable();
        using var alumno = CrearCliente(sinRedireccion: true);
        using var fabricaDocente = new FabricaSabiaAlumno(
            fixture.ConexionAplicacion!, fixture.DirectorioClaves!, "docente-alumno-http");
        using var docente = fabricaDocente.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        using var fabricaDelegada = new FabricaSabiaAlumno(
            fixture.ConexionAplicacion!, fixture.DirectorioClaves!, UsuarioDocenteDelegadoId);
        using var delegada = fabricaDelegada.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        using var fabricaSinDelegacion = new FabricaSabiaAlumno(
            fixture.ConexionAplicacion!, fixture.DirectorioClaves!, UsuarioDocenteSinDelegacionId);
        using var sinDelegacion = fabricaSinDelegacion.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        using var fabricaSinPermiso = new FabricaSabiaAlumno(
            fixture.ConexionAplicacion!, fixture.DirectorioClaves!, UsuarioSinPermisoId);
        using var sinPermiso = fabricaSinPermiso.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var abacusPropio = await alumno.GetAsync(
            $"/Calificaciones/Abacus?id={escenario.ConfiguracionCalificacionesId}",
            TestContext.Current.CancellationToken);
        var abacusHistorico = await alumno.GetAsync(
            $"/Calificaciones/Abacus?id={escenario.ConfiguracionCalificacionesHistoricaId}",
            TestContext.Current.CancellationToken);
        var inexistente = await alumno.GetAsync(
            "/Calificaciones/Abacus?id=2147483647", TestContext.Current.CancellationToken);
        var ajeno = await alumno.GetAsync(
            $"/Calificaciones/Abacus?id={escenario.ConfiguracionCalificacionesAjenaId}",
            TestContext.Current.CancellationToken);
        var abacusDocente = await docente.GetAsync(
            $"/Calificaciones/Abacus?id={escenario.ConfiguracionCalificacionesId}",
            TestContext.Current.CancellationToken);
        var abacusDelegada = await delegada.GetAsync(
            $"/Calificaciones/Abacus?id={escenario.ConfiguracionCalificacionesId}",
            TestContext.Current.CancellationToken);
        var abacusSinDelegacion = await sinDelegacion.GetAsync(
            $"/Calificaciones/Abacus?id={escenario.ConfiguracionCalificacionesId}",
            TestContext.Current.CancellationToken);
        var abacusSinPermiso = await sinPermiso.GetAsync(
            $"/Calificaciones/Abacus?id={escenario.ConfiguracionCalificacionesId}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, abacusPropio.StatusCode);
        var contenidoAbacusPropio = await abacusPropio.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);
        Assert.Contains("ALU-HTTP-1", contenidoAbacusPropio);
        Assert.DoesNotContain("ALU-HTTP-3", contenidoAbacusPropio);

        Assert.Equal(HttpStatusCode.Found, abacusHistorico.StatusCode);
        Assert.Equal("/Calificaciones", abacusHistorico.Headers.Location?.OriginalString);

        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, ajeno.StatusCode);
        Assert.Null(inexistente.Headers.Location);
        Assert.Null(ajeno.Headers.Location);
        var contenidoNoDisponible = await inexistente.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);
        Assert.Equal(contenidoNoDisponible, await ajeno.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken));
        Assert.DoesNotContain("Curso ajeno alumno HTTP", contenidoNoDisponible);

        Assert.Equal(HttpStatusCode.OK, abacusDocente.StatusCode);
        Assert.Equal(HttpStatusCode.OK, abacusDelegada.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, abacusSinDelegacion.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, abacusSinPermiso.StatusCode);

        var indice = await alumno.GetAsync("/Calificaciones", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, indice.StatusCode);
        var contenidoIndice = await indice.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains(
            "El resumen Abacus solicitado pertenece a otro ciclo escolar. Seleccione ese ciclo para consultarlo.",
            contenidoIndice);
        Assert.Contains("Curso alumno HTTP", contenidoIndice);
        Assert.DoesNotContain("Curso historico alumno HTTP", contenidoIndice);

        var segundaConsultaHistorica = await alumno.GetAsync(
            $"/Calificaciones/Abacus?id={escenario.ConfiguracionCalificacionesHistoricaId}",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Found, segundaConsultaHistorica.StatusCode);
        Assert.Equal("/Calificaciones", segundaConsultaHistorica.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task DetalleEntrega_DistingueSoloLaEntregaAutorizadaDeOtroCiclo_YConservaElCicloSeleccionado()
    {
        OmitirSinPostgresDescartable();
        using var cliente = CrearCliente(sinRedireccion: true);

        var inexistente = await cliente.GetAsync(
            "/Entregas/Detalle?id=2147483647", TestContext.Current.CancellationToken);
        var ajena = await cliente.GetAsync(
            $"/Entregas/Detalle?id={escenario.EntregaAjenaId}", TestContext.Current.CancellationToken);
        var ajenaEnOtroCiclo = await cliente.GetAsync(
            $"/Entregas/Detalle?id={escenario.EntregaHistoricaAjenaId}",
            TestContext.Current.CancellationToken);
        var autorizadaEnOtroCiclo = await cliente.GetAsync(
            $"/Entregas/Detalle?id={escenario.EntregaHistoricaPropiaId}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, ajena.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, ajenaEnOtroCiclo.StatusCode);
        Assert.Null(inexistente.Headers.Location);
        Assert.Null(ajena.Headers.Location);
        Assert.Null(ajenaEnOtroCiclo.Headers.Location);
        var contenidoNoDisponible = await inexistente.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal(contenidoNoDisponible,
            await ajena.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(contenidoNoDisponible,
            await ajenaEnOtroCiclo.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.Found, autorizadaEnOtroCiclo.StatusCode);
        Assert.Equal("/Entregas", autorizadaEnOtroCiclo.Headers.Location?.OriginalString);

        var indice = await cliente.GetAsync("/Entregas", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, indice.StatusCode);
        Assert.Contains(
            "La entrega solicitada no corresponde al ciclo escolar seleccionado.",
            await indice.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        var segundaConsulta = await cliente.GetAsync(
            $"/Entregas/Detalle?id={escenario.EntregaHistoricaPropiaId}",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Found, segundaConsulta.StatusCode);
    }

    [Fact]
    public async Task UsuarioSinPermisoGeneral_RecibeForbiddenAntesDeConsultarElDetalle()
    {
        OmitirSinPostgresDescartable();
        using var fabricaSinPermiso = new FabricaSabiaAlumno(
            fixture.ConexionAplicacion!, fixture.DirectorioClaves!, UsuarioSinPermisoId);
        using var cliente = fabricaSinPermiso.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var respuesta = await cliente.GetAsync(
            $"/Entregas/Detalle?id={escenario.EntregaPendienteId}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task DetalleEntrega_DelegacionesRespetanLasOperacionesEfectivas()
    {
        OmitirSinPostgresDescartable();
        using var fabricaDelegada = new FabricaSabiaAlumno(
            fixture.ConexionAplicacion!, fixture.DirectorioClaves!, UsuarioDocenteDelegadoId);
        using var clienteDelegada = fabricaDelegada.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        using var fabricaSinDelegacion = new FabricaSabiaAlumno(
            fixture.ConexionAplicacion!, fixture.DirectorioClaves!, UsuarioDocenteSinDelegacionId);
        using var clienteSinDelegacion = fabricaSinDelegacion.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var autorizada = await clienteDelegada.GetAsync(
            $"/Entregas/Detalle?id={escenario.EntregaPendienteId}",
            TestContext.Current.CancellationToken);
        var noAutorizada = await clienteSinDelegacion.GetAsync(
            $"/Entregas/Detalle?id={escenario.EntregaPendienteId}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, autorizada.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, noAutorizada.StatusCode);
        Assert.Null(noAutorizada.Headers.Location);
    }

    [Fact]
    public async Task CalificacionesIndex_RestringeLaListaAlAmbitoAcademicoEfectivo()
    {
        OmitirSinPostgresDescartable();
        using var alumno = CrearCliente(sinRedireccion: true);
        using var fabricaDocente = new FabricaSabiaAlumno(
            fixture.ConexionAplicacion!, fixture.DirectorioClaves!, "docente-alumno-http");
        using var docente = fabricaDocente.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        using var fabricaDelegada = new FabricaSabiaAlumno(
            fixture.ConexionAplicacion!, fixture.DirectorioClaves!, UsuarioDocenteDelegadoId);
        using var delegada = fabricaDelegada.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        using var fabricaSinDelegacion = new FabricaSabiaAlumno(
            fixture.ConexionAplicacion!, fixture.DirectorioClaves!, UsuarioDocenteSinDelegacionId);
        using var sinDelegacion = fabricaSinDelegacion.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        using var fabricaOperador = new FabricaSabiaAlumno(
            fixture.ConexionAplicacion!, fixture.DirectorioClaves!, UsuarioOperadorCalificacionesId);
        using var operador = fabricaOperador.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        using var fabricaSoloVer = new FabricaSabiaAlumno(
            fixture.ConexionAplicacion!, fixture.DirectorioClaves!, UsuarioSoloVerCalificacionesId);
        using var soloVer = fabricaSoloVer.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        using var fabricaSinPermiso = new FabricaSabiaAlumno(
            fixture.ConexionAplicacion!, fixture.DirectorioClaves!, UsuarioSinPermisoId);
        using var sinPermiso = fabricaSinPermiso.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var respuestaAlumno = await alumno.GetAsync("/Calificaciones", TestContext.Current.CancellationToken);
        var respuestaDocente = await docente.GetAsync("/Calificaciones", TestContext.Current.CancellationToken);
        var respuestaDelegada = await delegada.GetAsync("/Calificaciones", TestContext.Current.CancellationToken);
        var respuestaSinDelegacion = await sinDelegacion.GetAsync("/Calificaciones", TestContext.Current.CancellationToken);
        var respuestaOperador = await operador.GetAsync("/Calificaciones", TestContext.Current.CancellationToken);
        var respuestaSoloVer = await soloVer.GetAsync("/Calificaciones", TestContext.Current.CancellationToken);
        var respuestaSinPermiso = await sinPermiso.GetAsync("/Calificaciones", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, respuestaAlumno.StatusCode);
        Assert.Equal(HttpStatusCode.OK, respuestaDocente.StatusCode);
        Assert.Equal(HttpStatusCode.OK, respuestaDelegada.StatusCode);
        Assert.Equal(HttpStatusCode.OK, respuestaSinDelegacion.StatusCode);
        Assert.Equal(HttpStatusCode.OK, respuestaOperador.StatusCode);
        Assert.Equal(HttpStatusCode.OK, respuestaSoloVer.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, respuestaSinPermiso.StatusCode);

        var contenidoAlumno = await respuestaAlumno.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var contenidoDocente = await respuestaDocente.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var contenidoDelegada = await respuestaDelegada.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var contenidoSinDelegacion = await respuestaSinDelegacion.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var contenidoOperador = await respuestaOperador.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var contenidoSoloVer = await respuestaSoloVer.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        var filasAlumno = ExtraerFilasIndiceCalificaciones(contenidoAlumno);
        var filasDocente = ExtraerFilasIndiceCalificaciones(contenidoDocente);
        var filasDelegada = ExtraerFilasIndiceCalificaciones(contenidoDelegada);
        var filasSinDelegacion = ExtraerFilasIndiceCalificaciones(contenidoSinDelegacion);
        var filasOperador = ExtraerFilasIndiceCalificaciones(contenidoOperador);
        var filasSoloVer = ExtraerFilasIndiceCalificaciones(contenidoSoloVer);

        Assert.Contains("Curso alumno HTTP", filasAlumno);
        Assert.DoesNotContain("Curso ajeno alumno HTTP", filasAlumno);
        Assert.Contains("Curso alumno HTTP", filasDocente);
        Assert.DoesNotContain("Curso ajeno alumno HTTP", filasDocente);
        Assert.Contains("Curso alumno HTTP", filasDelegada);
        Assert.DoesNotContain("Curso ajeno alumno HTTP", filasDelegada);
        Assert.DoesNotContain("Curso alumno HTTP", filasSinDelegacion);
        Assert.Contains("Curso ajeno alumno HTTP", filasSinDelegacion);
        Assert.Contains("Curso alumno HTTP", filasOperador);
        Assert.Contains("Curso ajeno alumno HTTP", filasOperador);
        Assert.DoesNotContain("Curso alumno HTTP", filasSoloVer);
        Assert.DoesNotContain("Curso ajeno alumno HTTP", filasSoloVer);

        var opcionesDocente = ExtraerOpcionesAsignacion(contenidoDocente);
        var opcionesDelegada = ExtraerOpcionesAsignacion(contenidoDelegada);
        var opcionesSinDelegacion = ExtraerOpcionesAsignacion(contenidoSinDelegacion);

        Assert.Contains("Curso alumno HTTP", opcionesDocente);
        Assert.DoesNotContain("Curso ajeno alumno HTTP", opcionesDocente);
        Assert.Contains("Curso alumno HTTP", opcionesDelegada);
        Assert.DoesNotContain("Curso ajeno alumno HTTP", opcionesDelegada);
        Assert.DoesNotContain("Curso alumno HTTP", opcionesSinDelegacion);
        Assert.Contains("Curso ajeno alumno HTTP", opcionesSinDelegacion);
    }

    private static string ExtraerFilasIndiceCalificaciones(string contenido)
    {
        var coincidencia = Regex.Match(contenido,
            @"<table\b(?=[^>]*\bdata-tabla-index\b)[^>]*>.*?<tbody\b[^>]*>(?<filas>.*?)</tbody>",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);
        return coincidencia.Success ? coincidencia.Groups["filas"].Value : string.Empty;
    }

    private static string ExtraerOpcionesAsignacion(string contenido)
    {
        var coincidencia = Regex.Match(contenido,
            @"<select\b(?=[^>]*\bid\s*=\s*[\""']asignacionId[\""'])[^>]*>(?<opciones>.*?)</select>",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);
        return coincidencia.Success ? coincidencia.Groups["opciones"].Value : string.Empty;
    }

    private HttpClient CrearCliente(bool sinRedireccion = false) =>
        fixture.CrearCliente(sinRedireccion);

    private void OmitirSinPostgresDescartable() =>
        fixture.OmitirSinPostgresDescartable();

    public sealed class Fixture : IAsyncLifetime
    {
        private readonly string? conexionAdministrador =
            Environment.GetEnvironmentVariable("SABIA_PRUEBAS_PG");
        private readonly bool destinoConfirmado =
            Environment.GetEnvironmentVariable("SABIA_PRUEBAS_PG_CONFIRMAR") == ConfirmacionEsperada;
        private string? nombreBase;
        private FabricaSabiaAlumno? fabrica;

        internal string? ConexionAplicacion { get; private set; }
        internal string? DirectorioClaves { get; private set; }
        internal EscenarioAlumno Escenario { get; private set; } = null!;

        public async ValueTask InitializeAsync()
        {
            if (string.IsNullOrWhiteSpace(conexionAdministrador) || !destinoConfirmado)
            {
                return;
            }

            nombreBase = "sabia_prueba_alumno_" + Guid.NewGuid().ToString("N")[..10];
            var constructor = new NpgsqlConnectionStringBuilder(conexionAdministrador)
            {
                Database = nombreBase,
                Pooling = false
            };
            ConexionAplicacion = constructor.ConnectionString;
            DirectorioClaves = Path.Combine(Path.GetTempPath(), "sabia-alumno-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(DirectorioClaves);

            await using var administrador = new NpgsqlConnection(conexionAdministrador);
            await administrador.OpenAsync(TestContext.Current.CancellationToken);
            await using var comando = new NpgsqlCommand($"CREATE DATABASE \"{nombreBase}\"", administrador);
            await comando.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

            var opcionesContexto = new DbContextOptionsBuilder<Contexto>()
                .UseNpgsql(ConexionAplicacion)
                .Options;
            await using (var contextoMigracion = new Contexto(opcionesContexto))
            {
                await contextoMigracion.Database.MigrateAsync(TestContext.Current.CancellationToken);
            }

            fabrica = new FabricaSabiaAlumno(ConexionAplicacion, DirectorioClaves, UsuarioAlumnoId);
            using var ambito = fabrica.Services.CreateScope();
            Escenario = await EscenarioAlumno.CrearAsync(ambito.ServiceProvider);
        }

        public async ValueTask DisposeAsync()
        {
            fabrica?.Dispose();

            if (!string.IsNullOrWhiteSpace(conexionAdministrador) && !string.IsNullOrWhiteSpace(nombreBase))
            {
                NpgsqlConnection.ClearAllPools();
                await using var administrador = new NpgsqlConnection(conexionAdministrador);
                await administrador.OpenAsync();
                await using var comando = new NpgsqlCommand(
                    $"DROP DATABASE IF EXISTS \"{nombreBase}\" WITH (FORCE)", administrador);
                await comando.ExecuteNonQueryAsync();
            }

            if (!string.IsNullOrWhiteSpace(DirectorioClaves) && Directory.Exists(DirectorioClaves))
            {
                Directory.Delete(DirectorioClaves, recursive: true);
            }
        }

        internal HttpClient CrearCliente(bool sinRedireccion)
        {
            Assert.NotNull(fabrica);
            return fabrica!.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = !sinRedireccion
            });
        }

        internal void OmitirSinPostgresDescartable() =>
            Assert.SkipWhen(
                string.IsNullOrWhiteSpace(conexionAdministrador) || !destinoConfirmado,
                "PostgreSQL descartable no configurado o no confirmado.");
    }

    private sealed class FabricaSabiaAlumno(
        string conexion,
        string directorioClaves,
        string usuarioId)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration(configuracion =>
                configuracion.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:ConexionPrincipal"] = conexion,
                    ["SuperusuarioInicial:Usuario"] = "superusuario_alumno_prueba",
                    ["SuperusuarioInicial:Correo"] = "superusuario.alumno@example.test",
                    ["SuperusuarioInicial:Contrasena"] = $"A!a1{Guid.NewGuid():N}",
                    ["Correo:Habilitado"] = "false",
                    ["Logging:LogLevel:Default"] = "Warning",
                    ["Logging:LogLevel:Microsoft.EntityFrameworkCore.Database.Command"] = "Warning",
                    ["Logging:EventLog:LogLevel:Default"] = "None"
                }));
            builder.ConfigureServices(servicios =>
            {
                servicios.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(directorioClaves));
                servicios.AddAuthentication(opciones =>
                {
                    opciones.DefaultAuthenticateScheme = AutenticacionAlumno.Esquema;
                    opciones.DefaultChallengeScheme = AutenticacionAlumno.Esquema;
                    opciones.DefaultForbidScheme = AutenticacionAlumno.Esquema;
                }).AddScheme<OpcionesAutenticacionAlumno, AutenticacionAlumno>(
                    AutenticacionAlumno.Esquema,
                    opciones => opciones.UsuarioId = usuarioId);
            });
        }
    }

    private sealed class OpcionesAutenticacionAlumno : AuthenticationSchemeOptions
    {
        public string UsuarioId { get; set; } = string.Empty;
    }

    private sealed class AutenticacionAlumno(
        IOptionsMonitor<OpcionesAutenticacionAlumno> opciones,
        ILoggerFactory logger,
        UrlEncoder codificador,
        UserManager<Usuario> usuarios,
        RoleManager<Rol> roles)
        : AuthenticationHandler<OpcionesAutenticacionAlumno>(opciones, logger, codificador)
    {
        public const string Esquema = "AlumnoPrueba";

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var usuario = await usuarios.FindByIdAsync(Options.UsuarioId);
            if (usuario == null)
            {
                return AuthenticateResult.Fail("El usuario temporal no existe.");
            }

            var nombresRoles = await usuarios.GetRolesAsync(usuario);
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, usuario.Id),
                new(ClaimTypes.Name, usuario.UserName ?? usuario.Id)
            };
            foreach (var nombreRol in nombresRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, nombreRol));
                var rol = await roles.FindByNameAsync(nombreRol);
                if (rol != null)
                {
                    claims.AddRange(await roles.GetClaimsAsync(rol));
                }
            }

            var identidad = new ClaimsIdentity(claims, Esquema);
            return AuthenticateResult.Success(new AuthenticationTicket(
                new ClaimsPrincipal(identidad), Esquema));
        }
    }

    internal sealed class EscenarioAlumno
    {
        public int AlumnoAjenoId { get; private init; }
        public int AlumnoCompaneroId { get; private init; }
        public int AlumnoPropioId { get; private init; }
        public int AsignacionAjenaId { get; private init; }
        public int AsignacionPropiaId { get; private init; }
        public int ConfiguracionCalificacionesId { get; private init; }
        public int ConfiguracionCalificacionesAjenaId { get; private init; }
        public int ConfiguracionCalificacionesHistoricaId { get; private init; }
        public int CuestionarioAjenoId { get; private init; }
        public int CuestionarioPropioId { get; private init; }
        public int EntregaAjenaId { get; private init; }
        public int EntregaHistoricaAjenaId { get; private init; }
        public int EntregaHistoricaPropiaId { get; private init; }
        public int EntregaPendienteId { get; private init; }
        public int PeriodoId { get; private init; }

        public static async Task<EscenarioAlumno> CrearAsync(IServiceProvider servicios)
        {
            var usuarios = servicios.GetRequiredService<UserManager<Usuario>>();
            var roles = servicios.GetRequiredService<RoleManager<Rol>>();
            var contexto = servicios.GetRequiredService<Contexto>();

            var rolAlumno = await roles.FindByNameAsync(Roles.Alumno);
            Assert.NotNull(rolAlumno);
            var permisos = await roles.GetClaimsAsync(rolAlumno!);
            Assert.Contains(permisos, x => x.Type == TiposClaims.Permiso && x.Value == Permisos.Entregas.Entregar);
            Assert.Contains(permisos, x => x.Type == TiposClaims.Permiso && x.Value == Permisos.Cuestionarios.Resolver);
            Assert.DoesNotContain(permisos, x => x.Type == TiposClaims.Permiso && x.Value == Permisos.Tareas.Crear);
            Assert.DoesNotContain(permisos, x => x.Type == TiposClaims.Permiso && x.Value == Permisos.Calificaciones.Configurar);

            await CrearUsuarioAsync(usuarios, UsuarioAlumnoId, "alumno.http", "Alumno", "HTTP");
            await CrearUsuarioAsync(usuarios, UsuarioSinPermisoId, "sin.permiso.http", "Sin permiso", "HTTP");
            await CrearUsuarioAsync(usuarios, UsuarioOperadorCalificacionesId, "operador.calificaciones.http", "Operador", "Calificaciones");
            await CrearUsuarioAsync(usuarios, UsuarioSoloVerCalificacionesId, "solo.ver.calificaciones.http", "Solo ver", "Calificaciones");
            await CrearUsuarioAsync(usuarios, "alumno-ajeno-http", "alumno.ajeno.http", "Alumno", "Ajeno");
            await CrearUsuarioAsync(usuarios, "alumno-companero-http", "alumno.companero.http", "Compañero", "Expuesto");
            await CrearUsuarioAsync(usuarios, "docente-alumno-http", "docente.alumno.http", "Docente", "Alumno");
            await CrearUsuarioAsync(usuarios, UsuarioDocenteSinDelegacionId, "docente.ajeno.alumno.http", "Docente", "Ajeno");
            await CrearUsuarioAsync(usuarios, UsuarioDocenteDelegadoId, "docente.delegado.http", "Docente", "Delegado");
            var rolAsignado = await usuarios.AddToRoleAsync(
                (await usuarios.FindByIdAsync(UsuarioAlumnoId))!, Roles.Alumno);
            Assert.True(rolAsignado.Succeeded, string.Join("; ", rolAsignado.Errors.Select(x => x.Description)));
            var rolDelegado = await usuarios.AddToRoleAsync(
                (await usuarios.FindByIdAsync(UsuarioDocenteDelegadoId))!, Roles.Docente);
            Assert.True(rolDelegado.Succeeded, string.Join("; ", rolDelegado.Errors.Select(x => x.Description)));
            var rolSinDelegacion = await usuarios.AddToRoleAsync(
                (await usuarios.FindByIdAsync(UsuarioDocenteSinDelegacionId))!, Roles.Docente);
            Assert.True(rolSinDelegacion.Succeeded,
                string.Join("; ", rolSinDelegacion.Errors.Select(x => x.Description)));
            var rolDocente = await usuarios.AddToRoleAsync(
                (await usuarios.FindByIdAsync("docente-alumno-http"))!, Roles.Docente);
            Assert.True(rolDocente.Succeeded,
                string.Join("; ", rolDocente.Errors.Select(x => x.Description)));
            var rolSoloVer = new Rol { Name = "SoloVerCalificacionesHttp", Activo = true };
            var crearRolSoloVer = await roles.CreateAsync(rolSoloVer);
            Assert.True(crearRolSoloVer.Succeeded,
                string.Join("; ", crearRolSoloVer.Errors.Select(x => x.Description)));
            var permisoSoloVer = await roles.AddClaimAsync(rolSoloVer,
                new Claim(TiposClaims.Permiso, Permisos.Calificaciones.Ver));
            Assert.True(permisoSoloVer.Succeeded,
                string.Join("; ", permisoSoloVer.Errors.Select(x => x.Description)));
            var asignarSoloVer = await usuarios.AddToRoleAsync(
                (await usuarios.FindByIdAsync(UsuarioSoloVerCalificacionesId))!, rolSoloVer.Name!);
            Assert.True(asignarSoloVer.Succeeded,
                string.Join("; ", asignarSoloVer.Errors.Select(x => x.Description)));
            var rolOperador = new Rol { Name = "OperadorCalificacionesHttp", Activo = true };
            var crearRolOperador = await roles.CreateAsync(rolOperador);
            Assert.True(crearRolOperador.Succeeded,
                string.Join("; ", crearRolOperador.Errors.Select(x => x.Description)));
            foreach (var permiso in new[]
            {
                Permisos.Calificaciones.Ver,
                Permisos.Dashboard.CoordinacionVer,
                Permisos.Planificaciones.Revisar
            })
            {
                var resultadoPermiso = await roles.AddClaimAsync(rolOperador,
                    new Claim(TiposClaims.Permiso, permiso));
                Assert.True(resultadoPermiso.Succeeded,
                    string.Join("; ", resultadoPermiso.Errors.Select(x => x.Description)));
            }
            var asignarOperador = await usuarios.AddToRoleAsync(
                (await usuarios.FindByIdAsync(UsuarioOperadorCalificacionesId))!, rolOperador.Name!);
            Assert.True(asignarOperador.Succeeded,
                string.Join("; ", asignarOperador.Errors.Select(x => x.Description)));
            var ciclo = new CicloEscolar
            {
                Anio = 2026,
                FechaInicio = new DateOnly(2026, 1, 1),
                FechaFin = new DateOnly(2026, 11, 30),
                Activo = true,
                Estado = EstadoCicloEscolar.Activo
            };
            var cicloHistorico = new CicloEscolar
            {
                Anio = 2025,
                FechaInicio = new DateOnly(2025, 1, 1),
                FechaFin = new DateOnly(2025, 11, 30),
                Activo = false,
                Estado = EstadoCicloEscolar.Cerrado
            };
            var grado = new Grado
            {
                Nombre = "Sexto",
                Carrera = "Bachillerato en Computacion",
                Nivel = NivelEducativo.Diversificado,
                Orden = 6
            };
            contexto.AddRange(ciclo, cicloHistorico, grado);
            await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
            var seccionPropia = new Seccion { CicloEscolarId = ciclo.Id, GradoId = grado.Id, Nombre = "A" };
            var seccionAjena = new Seccion { CicloEscolarId = ciclo.Id, GradoId = grado.Id, Nombre = "B" };
            var seccionHistorica = new Seccion { CicloEscolarId = cicloHistorico.Id, GradoId = grado.Id, Nombre = "H" };
            var cursoPropio = new Curso { GradoId = grado.Id, Nombre = "Curso alumno HTTP" };
            var cursoAjeno = new Curso { GradoId = grado.Id, Nombre = "Curso ajeno alumno HTTP" };
            var cursoHistorico = new Curso { GradoId = grado.Id, Nombre = "Curso historico alumno HTTP" };
            var docentePropio = new Docente { UsuarioId = "docente-alumno-http", Carnet = "DOC-ALU-1" };
            var docenteAjeno = new Docente { UsuarioId = UsuarioDocenteSinDelegacionId, Carnet = "DOC-ALU-2" };
            var docenteDelegado = new Docente { UsuarioId = UsuarioDocenteDelegadoId, Carnet = "DOC-ALU-3" };
            var alumnoPropio = new Alumno { UsuarioId = UsuarioAlumnoId, CodigoPersonal = "ALU-HTTP-1" };
            var alumnoAjeno = new Alumno { UsuarioId = "alumno-ajeno-http", CodigoPersonal = "ALU-HTTP-2" };
            var alumnoCompanero = new Alumno { UsuarioId = "alumno-companero-http", CodigoPersonal = "ALU-HTTP-3" };
            var periodo = new Periodo
            {
                CicloEscolarId = ciclo.Id,
                Numero = 1,
                Nombre = "Primer bimestre",
                FechaInicio = new DateOnly(2026, 1, 1),
                FechaFin = new DateOnly(2026, 3, 15)
            };
            var periodoHistorico = new Periodo
            {
                CicloEscolarId = cicloHistorico.Id,
                Numero = 1,
                Nombre = "Primer bimestre historico",
                FechaInicio = new DateOnly(2025, 1, 1),
                FechaFin = new DateOnly(2025, 3, 15)
            };
            contexto.AddRange(seccionPropia, seccionAjena, seccionHistorica, cursoPropio, cursoAjeno,
                cursoHistorico, docentePropio, docenteAjeno, docenteDelegado, alumnoPropio, alumnoAjeno,
                alumnoCompanero, periodo, periodoHistorico);
            await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
            var asignacionPropia = new Asignacion
            {
                SeccionId = seccionPropia.Id,
                CursoId = cursoPropio.Id,
                GradoId = grado.Id,
                DocenteId = docentePropio.Id
            };
            var asignacionAjena = new Asignacion
            {
                SeccionId = seccionAjena.Id,
                CursoId = cursoAjeno.Id,
                GradoId = grado.Id,
                DocenteId = docenteAjeno.Id
            };
            var asignacionHistorica = new Asignacion
            {
                SeccionId = seccionHistorica.Id,
                CursoId = cursoHistorico.Id,
                GradoId = grado.Id,
                DocenteId = docentePropio.Id
            };
            contexto.AddRange(asignacionPropia, asignacionAjena, asignacionHistorica);
            await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

            var relevo = new RelevoAsignacion
            {
                AsignacionAnteriorId = asignacionPropia.Id,
                AsignacionNuevaId = asignacionPropia.Id,
                DocenteAnteriorId = docentePropio.Id,
                DocenteSustitutoId = docenteDelegado.Id,
                Tipo = TipoRelevoAsignacion.Excepcional,
                FechaEfectivaUtc = DateTime.UtcNow.AddDays(-1),
                Motivo = "Cobertura temporal para prueba HTTP",
                UsuarioResponsableId = "docente-alumno-http",
                FechaRegistroUtc = DateTime.UtcNow,
                ConsecuenciasConfirmadas = true
            };
            contexto.RelevosAsignaciones.Add(relevo);
            await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
            contexto.DelegacionesAcademicas.Add(new DelegacionAcademica
            {
                RelevoAsignacionId = relevo.Id,
                AsignacionId = asignacionPropia.Id,
                DocenteId = docenteDelegado.Id,
                Operaciones = OperacionDelegacionAcademica.CalificarEntregas |
                    OperacionDelegacionAcademica.GestionarEvaluaciones,
                FechaInicioUtc = DateTime.UtcNow.AddDays(-1),
                CreadaPorUsuarioId = "docente-alumno-http",
                FechaCreacionUtc = DateTime.UtcNow,
                Activa = true
            });
            await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
            var inscripcionPropia = new Inscripcion
            {
                AlumnoId = alumnoPropio.Id,
                SeccionId = seccionPropia.Id,
                CicloEscolarId = ciclo.Id,
                Fecha = new DateOnly(2026, 1, 15)
            };
            var inscripcionAjena = new Inscripcion
            {
                AlumnoId = alumnoAjeno.Id,
                SeccionId = seccionAjena.Id,
                CicloEscolarId = ciclo.Id,
                Fecha = new DateOnly(2026, 1, 15)
            };
            var inscripcionCompanero = new Inscripcion
            {
                AlumnoId = alumnoCompanero.Id,
                SeccionId = seccionPropia.Id,
                CicloEscolarId = ciclo.Id,
                Fecha = new DateOnly(2026, 1, 15)
            };
            var inscripcionHistorica = new Inscripcion
            {
                AlumnoId = alumnoPropio.Id,
                SeccionId = seccionHistorica.Id,
                CicloEscolarId = cicloHistorico.Id,
                Fecha = new DateOnly(2025, 1, 15)
            };
            var unidad = new Unidad
            {
                Titulo = "Unidad alumno HTTP",
                CreadoPorUsuarioId = "docente-alumno-http",
                FechaCreacion = DateTime.UtcNow
            };
            contexto.AddRange(inscripcionPropia, inscripcionAjena, inscripcionCompanero, inscripcionHistorica, unidad);
            await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
            contexto.UnidadesAsignaciones.Add(new UnidadAsignacion
            {
                UnidadId = unidad.Id,
                AsignacionId = asignacionPropia.Id,
                PeriodoId = periodo.Id,
                Orden = 1,
                Estado = EstadoPublicacionUnidad.Publicada,
                FechaDisponibilidad = DateTime.UtcNow.AddDays(-1),
                FechaPublicacion = DateTime.UtcNow,
                AsignadoPorUsuarioId = "docente-alumno-http"
            });
            var tareaArchivo = CrearTarea(asignacionPropia.Id, "Tarea entregable HTTP", "docente-alumno-http");
            var tareaHistorica = CrearTarea(asignacionHistorica.Id, "Tarea histórica HTTP", "docente-alumno-http");
            var tareaCuestionarioPropio = CrearTarea(asignacionPropia.Id, "Cuestionario propio HTTP", "docente-alumno-http", TipoTarea.Cuestionario);
            var tareaCuestionarioAjeno = CrearTarea(asignacionAjena.Id, "Cuestionario ajeno HTTP", "docente-ajeno-alumno-http", TipoTarea.Cuestionario);
            var tareaControl = CrearTarea(asignacionPropia.Id, "Tarea confidencial HTTP", "docente-alumno-http");
            tareaControl.Estado = EstadoTarea.Cerrada;
            tareaControl.FechaCierre = DateTime.UtcNow;
            contexto.AddRange(tareaArchivo, tareaHistorica, tareaCuestionarioPropio, tareaCuestionarioAjeno, tareaControl);
            await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
            var cuestionarioPropio = CrearCuestionario(tareaCuestionarioPropio.Id, "Pregunta propia HTTP");
            var cuestionarioAjeno = CrearCuestionario(tareaCuestionarioAjeno.Id, "Pregunta ajena HTTP");
            var entregaPendiente = new Entrega
            {
                TareaId = tareaArchivo.Id,
                AlumnoId = alumnoPropio.Id,
                InscripcionId = inscripcionPropia.Id,
                Estado = EstadoEntrega.Pendiente,
                NumeroEnvio = 1
            };
            var entregaAjena = new Entrega
            {
                TareaId = tareaCuestionarioAjeno.Id,
                AlumnoId = alumnoAjeno.Id,
                InscripcionId = inscripcionAjena.Id,
                Estado = EstadoEntrega.Pendiente,
                NumeroEnvio = 1
            };
            var entregaHistorica = new Entrega
            {
                TareaId = tareaHistorica.Id,
                AlumnoId = alumnoPropio.Id,
                InscripcionId = inscripcionHistorica.Id,
                Estado = EstadoEntrega.Enviada,
                NumeroEnvio = 1,
                FechaEntrega = DateTime.UtcNow.AddYears(-1)
            };
            var entregaHistoricaAjena = new Entrega
            {
                TareaId = tareaHistorica.Id,
                AlumnoId = alumnoAjeno.Id,
                InscripcionId = inscripcionAjena.Id,
                Estado = EstadoEntrega.Enviada,
                NumeroEnvio = 1,
                FechaEntrega = DateTime.UtcNow.AddYears(-1)
            };
            var entregaControlPropia = new Entrega
            {
                TareaId = tareaControl.Id,
                AlumnoId = alumnoPropio.Id,
                InscripcionId = inscripcionPropia.Id,
                Estado = EstadoEntrega.Calificada,
                NumeroEnvio = 1,
                Calificacion = 9
            };
            var entregaControlCompanero = new Entrega
            {
                TareaId = tareaControl.Id,
                AlumnoId = alumnoCompanero.Id,
                InscripcionId = inscripcionCompanero.Id,
                Estado = EstadoEntrega.Calificada,
                NumeroEnvio = 1,
                Calificacion = 6
            };
            contexto.AddRange(cuestionarioPropio, cuestionarioAjeno, entregaPendiente, entregaAjena,
                entregaHistorica, entregaHistoricaAjena, entregaControlPropia, entregaControlCompanero);
            await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
            var configuracionCalificaciones = new ConfiguracionEvaluacion
            {
                AsignacionId = asignacionPropia.Id,
                SeccionId = seccionPropia.Id,
                CicloEscolarId = ciclo.Id,
                PeriodoId = periodo.Id,
                MetodoCalculo = MetodoCalculoEvaluacion.CategoriasPonderadas,
                Estado = EstadoConfiguracionEvaluacion.Activa,
                FechaCreacion = DateTime.UtcNow,
                ConfiguradoPorUsuarioId = "docente-alumno-http"
            };
            var configuracionCalificacionesAjena = new ConfiguracionEvaluacion
            {
                AsignacionId = asignacionAjena.Id,
                SeccionId = seccionAjena.Id,
                CicloEscolarId = ciclo.Id,
                PeriodoId = periodo.Id,
                MetodoCalculo = MetodoCalculoEvaluacion.CategoriasPonderadas,
                Estado = EstadoConfiguracionEvaluacion.Activa,
                FechaCreacion = DateTime.UtcNow,
                ConfiguradoPorUsuarioId = UsuarioDocenteSinDelegacionId
            };
            var configuracionCalificacionesHistorica = new ConfiguracionEvaluacion
            {
                AsignacionId = asignacionHistorica.Id,
                SeccionId = seccionHistorica.Id,
                CicloEscolarId = cicloHistorico.Id,
                PeriodoId = periodoHistorico.Id,
                MetodoCalculo = MetodoCalculoEvaluacion.CategoriasPonderadas,
                Estado = EstadoConfiguracionEvaluacion.Activa,
                FechaCreacion = DateTime.UtcNow,
                ConfiguradoPorUsuarioId = "docente-alumno-http"
            };
            contexto.ConfiguracionesEvaluacion.AddRange(
                configuracionCalificaciones, configuracionCalificacionesAjena,
                configuracionCalificacionesHistorica);
            await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
            var categoriaPropia = new CategoriaEvaluacion
            {
                ConfiguracionEvaluacionId = configuracionCalificaciones.Id,
                Nombre = "DesempeÃ±o",
                Tipo = TipoCategoriaEvaluacion.Desempeno,
                Porcentaje = 100,
                Orden = 1
            };
            var categoriaAjena = new CategoriaEvaluacion
            {
                ConfiguracionEvaluacionId = configuracionCalificacionesAjena.Id,
                Nombre = "DesempeÃ±o",
                Tipo = TipoCategoriaEvaluacion.Desempeno,
                Porcentaje = 100,
                Orden = 1
            };
            var categoriaHistorica = new CategoriaEvaluacion
            {
                ConfiguracionEvaluacionId = configuracionCalificacionesHistorica.Id,
                Nombre = "DesempeÃ±o",
                Tipo = TipoCategoriaEvaluacion.Desempeno,
                Porcentaje = 100,
                Orden = 1
            };
            contexto.CategoriasEvaluacion.AddRange(categoriaPropia, categoriaAjena, categoriaHistorica);
            await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
            contexto.ActividadesEvaluables.AddRange(
                new ActividadEvaluable
                {
                    ConfiguracionEvaluacionId = configuracionCalificaciones.Id,
                    AsignacionId = asignacionPropia.Id,
                    CategoriaEvaluacionId = categoriaPropia.Id,
                    Origen = OrigenActividadEvaluable.Manual,
                    Nombre = "Actividad de prueba",
                    PunteoMaximo = 100,
                    Orden = 1,
                    Activa = true,
                    FechaCreacion = DateTime.UtcNow
                },
                new ActividadEvaluable
                {
                    ConfiguracionEvaluacionId = configuracionCalificacionesAjena.Id,
                    AsignacionId = asignacionAjena.Id,
                    CategoriaEvaluacionId = categoriaAjena.Id,
                    Origen = OrigenActividadEvaluable.Manual,
                    Nombre = "Actividad de prueba",
                    PunteoMaximo = 100,
                    Orden = 1,
                    Activa = true,
                    FechaCreacion = DateTime.UtcNow
                },
                new ActividadEvaluable
                {
                    ConfiguracionEvaluacionId = configuracionCalificacionesHistorica.Id,
                    AsignacionId = asignacionHistorica.Id,
                    CategoriaEvaluacionId = categoriaHistorica.Id,
                    Origen = OrigenActividadEvaluable.Manual,
                    Nombre = "Actividad historica de prueba",
                    PunteoMaximo = 100,
                    Orden = 1,
                    Activa = true,
                    FechaCreacion = DateTime.UtcNow
                });
            contexto.CierresCalificaciones.Add(new CierreCalificaciones
            {
                ConfiguracionEvaluacionId = configuracionCalificaciones.Id,
                Estado = EstadoCierreCalificaciones.Cerrado,
                FechaCierre = DateTime.UtcNow,
                CerradoPorUsuarioId = "docente-alumno-http"
            });
            contexto.ResultadosCalificacionesPeriodos.AddRange(
                CrearResultado(configuracionCalificaciones.Id, inscripcionPropia.Id, alumnoPropio.Id, 9),
                CrearResultado(configuracionCalificaciones.Id, inscripcionCompanero.Id, alumnoCompanero.Id, 6));
            await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
            return new EscenarioAlumno
            {
                AlumnoAjenoId = alumnoAjeno.Id,
                AlumnoCompaneroId = alumnoCompanero.Id,
                AlumnoPropioId = alumnoPropio.Id,
                AsignacionAjenaId = asignacionAjena.Id,
                AsignacionPropiaId = asignacionPropia.Id,
                ConfiguracionCalificacionesId = configuracionCalificaciones.Id,
                ConfiguracionCalificacionesAjenaId = configuracionCalificacionesAjena.Id,
                ConfiguracionCalificacionesHistoricaId = configuracionCalificacionesHistorica.Id,
                CuestionarioAjenoId = cuestionarioAjeno.Id,
                CuestionarioPropioId = cuestionarioPropio.Id,
                EntregaAjenaId = entregaAjena.Id,
                EntregaHistoricaAjenaId = entregaHistoricaAjena.Id,
                EntregaHistoricaPropiaId = entregaHistorica.Id,
                EntregaPendienteId = entregaPendiente.Id,
                PeriodoId = periodo.Id
            };
        }

        private static ResultadoCalificacionPeriodo CrearResultado(int configuracionId, int inscripcionId,
            int alumnoId, decimal nota) => new()
        {
            ConfiguracionEvaluacionId = configuracionId,
            InscripcionId = inscripcionId,
            AlumnoId = alumnoId,
            Desempeno = nota,
            Actitudinal = nota,
            NotaBimestral = nota,
            AbacusDesempeno = nota,
            AbacusActitudinal = nota,
            FechaCierre = DateTime.UtcNow
        };

        private static Tarea CrearTarea(int asignacionId, string titulo, string creadoPor, TipoTarea tipo = TipoTarea.Archivo) => new()
        {
            AsignacionId = asignacionId,
            Tipo = tipo,
            Titulo = titulo,
            Estado = EstadoTarea.Publicada,
            PunteoMaximo = 10,
            FechaDisponibilidad = DateTime.UtcNow.AddDays(-1),
            FechaLimite = DateTime.UtcNow.AddDays(3),
            FechaCreacion = DateTime.UtcNow,
            FechaPublicacion = DateTime.UtcNow,
            CreadoPorUsuarioId = creadoPor,
            GrupoVersionId = Guid.NewGuid()
        };

        private static Cuestionario CrearCuestionario(int tareaId, string enunciado) => new()
        {
            TareaId = tareaId,
            MaximoIntentos = 1,
            PermitirNavegacion = true,
            Preguntas =
            [
                new Pregunta
                {
                    Orden = 1,
                    Tipo = TipoPregunta.SeleccionUnica,
                    Enunciado = enunciado,
                    Punteo = 10,
                    Opciones =
                    [
                        new OpcionPregunta { Orden = 1, Texto = "Correcta", EsCorrecta = true },
                        new OpcionPregunta { Orden = 2, Texto = "Incorrecta" }
                    ]
                }
            ]
        };

        private static async Task CrearUsuarioAsync(
            UserManager<Usuario> usuarios,
            string id,
            string nombreUsuario,
            string primerNombre,
            string primerApellido)
        {
            var resultado = await usuarios.CreateAsync(new Usuario
            {
                Id = id,
                UserName = nombreUsuario,
                Email = $"{nombreUsuario}@example.test",
                PrimerNombre = primerNombre,
                PrimerApellido = primerApellido,
                Activo = true,
                CambiarContrasena = false
            }, $"A!a1{Guid.NewGuid():N}");
            Assert.True(resultado.Succeeded, string.Join("; ", resultado.Errors.Select(x => x.Description)));
        }
    }
}

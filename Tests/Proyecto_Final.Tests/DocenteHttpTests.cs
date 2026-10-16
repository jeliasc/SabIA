using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
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
public sealed class DocenteHttpTests : IAsyncLifetime
{
    private const string ConfirmacionEsperada = "BASE_DESCARTABLE";
    private const string UsuarioDocenteId = "docente-http-prueba";
    private readonly string? conexionAdministrador =
        Environment.GetEnvironmentVariable("SABIA_PRUEBAS_PG");
    private readonly bool destinoConfirmado =
        Environment.GetEnvironmentVariable("SABIA_PRUEBAS_PG_CONFIRMAR") == ConfirmacionEsperada;
    private string? nombreBase;
    private string? conexionAplicacion;
    private string? directorioClaves;
    private FabricaSabiaDocente? fabrica;
    private EscenarioDocente escenario = null!;

    public async ValueTask InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(conexionAdministrador) || !destinoConfirmado)
        {
            return;
        }

        nombreBase = "sabia_prueba_docente_" + Guid.NewGuid().ToString("N")[..10];
        var constructor = new NpgsqlConnectionStringBuilder(conexionAdministrador)
        {
            Database = nombreBase,
            Pooling = false
        };
        conexionAplicacion = constructor.ConnectionString;
        directorioClaves = Path.Combine(Path.GetTempPath(), "sabia-docente-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directorioClaves);

        await using var administrador = new NpgsqlConnection(conexionAdministrador);
        await administrador.OpenAsync(TestContext.Current.CancellationToken);
        await using var comando = new NpgsqlCommand($"CREATE DATABASE \"{nombreBase}\"", administrador);
        await comando.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

        fabrica = new FabricaSabiaDocente(conexionAplicacion, directorioClaves, UsuarioDocenteId);
        using var ambito = fabrica.Services.CreateScope();
        escenario = await EscenarioDocente.CrearAsync(ambito.ServiceProvider);
    }

    [Fact]
    public async Task Docente_ConRolSembrado_VisualizaSoloSuContextoAcademico()
    {
        OmitirSinPostgresDescartable();
        using var cliente = CrearCliente();

        var dashboard = await cliente.GetAsync("/?contexto=docente", TestContext.Current.CancellationToken);
        var perfil = await cliente.GetAsync("/Docentes/Perfil", TestContext.Current.CancellationToken);
        var alumnos = await cliente.GetAsync("/Alumnos", TestContext.Current.CancellationToken);
        var crearUnidad = await cliente.GetAsync("/Unidades/Crear", TestContext.Current.CancellationToken);
        var crearPlanificacion = await cliente.GetAsync("/Planificaciones/Crear", TestContext.Current.CancellationToken);
        var crearTarea = await cliente.GetAsync("/Tareas/Crear", TestContext.Current.CancellationToken);
        var calificarPropia = await cliente.GetAsync(
            $"/Entregas/Calificar?id={escenario.EntregaPropiaId}", TestContext.Current.CancellationToken);
        var importarIa = await cliente.GetAsync(
            $"/Cuestionarios/ImportarIa?tareaId={escenario.TareaPropiaId}", TestContext.Current.CancellationToken);
        var notificaciones = await cliente.GetAsync("/Notificaciones", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
        var htmlDashboard = await dashboard.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("Mi dashboard docente", htmlDashboard);
        Assert.Contains("Curso propio HTTP", htmlDashboard);
        Assert.DoesNotContain("Curso ajeno HTTP", htmlDashboard);
        Assert.Equal(HttpStatusCode.OK, perfil.StatusCode);
        Assert.Equal(HttpStatusCode.OK, alumnos.StatusCode);
        Assert.Equal(HttpStatusCode.OK, crearUnidad.StatusCode);
        Assert.Equal(HttpStatusCode.OK, crearPlanificacion.StatusCode);
        Assert.Equal(HttpStatusCode.OK, crearTarea.StatusCode);
        Assert.Equal(HttpStatusCode.OK, calificarPropia.StatusCode);
        Assert.Equal(HttpStatusCode.OK, importarIa.StatusCode);
        Assert.Equal(HttpStatusCode.OK, notificaciones.StatusCode);
    }

    [Fact]
    public async Task Docente_NoAccedeARutasNiRecursosAjenos_YLosPostExigenAntiforgery()
    {
        OmitirSinPostgresDescartable();
        using var cliente = CrearCliente(sinRedireccion: true);

        var revisarPlanificacion = await cliente.GetAsync(
            $"/Planificaciones/Revisar?id={escenario.PlanificacionPropiaId}", TestContext.Current.CancellationToken);
        var administracionDocentes = await cliente.GetAsync("/Docentes/Index", TestContext.Current.CancellationToken);
        var alumnoAjeno = await cliente.GetAsync(
            $"/Alumnos/Ver?id={escenario.AlumnoAjenoId}", TestContext.Current.CancellationToken);
        var entregaAjena = await cliente.GetAsync(
            $"/Entregas/Calificar?id={escenario.EntregaAjenaId}", TestContext.Current.CancellationToken);
        var configuracionAjena = await cliente.GetAsync(
            $"/Calificaciones/Configurar?asignacionId={escenario.AsignacionAjenaId}&periodoId={escenario.PeriodoId}",
            TestContext.Current.CancellationToken);
        var postSinAntiforgery = await cliente.PostAsync(
            $"/Planificaciones/EnviarRevision?id={escenario.PlanificacionPropiaId}",
            content: null,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, revisarPlanificacion.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, administracionDocentes.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, alumnoAjeno.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, entregaAjena.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, configuracionAjena.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, postSinAntiforgery.StatusCode);
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

        if (!string.IsNullOrWhiteSpace(directorioClaves) && Directory.Exists(directorioClaves))
        {
            Directory.Delete(directorioClaves, recursive: true);
        }
    }

    private HttpClient CrearCliente(bool sinRedireccion = false)
    {
        Assert.NotNull(fabrica);
        return fabrica!.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = !sinRedireccion
        });
    }

    private void OmitirSinPostgresDescartable() =>
        Assert.SkipWhen(
            string.IsNullOrWhiteSpace(conexionAdministrador) || !destinoConfirmado,
            "PostgreSQL descartable no configurado o no confirmado.");

    private sealed class FabricaSabiaDocente(
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
                    ["SuperusuarioInicial:Usuario"] = "superusuario_docente_prueba",
                    ["SuperusuarioInicial:Correo"] = "superusuario.docente@example.test",
                    ["SuperusuarioInicial:Contrasena"] = $"A!a1{Guid.NewGuid():N}",
                    ["Correo:Habilitado"] = "false",
                    ["Logging:EventLog:LogLevel:Default"] = "None"
                }));
            builder.ConfigureServices(servicios =>
            {
                servicios.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(directorioClaves));
                servicios.AddAuthentication(opciones =>
                {
                    opciones.DefaultAuthenticateScheme = AutenticacionDocente.Esquema;
                    opciones.DefaultChallengeScheme = AutenticacionDocente.Esquema;
                    opciones.DefaultForbidScheme = AutenticacionDocente.Esquema;
                }).AddScheme<OpcionesAutenticacionDocente, AutenticacionDocente>(
                    AutenticacionDocente.Esquema,
                    opciones => opciones.UsuarioId = usuarioId);
            });
        }
    }

    private sealed class OpcionesAutenticacionDocente : AuthenticationSchemeOptions
    {
        public string UsuarioId { get; set; } = string.Empty;
    }

    private sealed class AutenticacionDocente(
        IOptionsMonitor<OpcionesAutenticacionDocente> opciones,
        ILoggerFactory logger,
        UrlEncoder codificador,
        UserManager<Usuario> usuarios,
        RoleManager<Rol> roles)
        : AuthenticationHandler<OpcionesAutenticacionDocente>(opciones, logger, codificador)
    {
        public const string Esquema = "DocentePrueba";

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
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identidad), Esquema);
            return AuthenticateResult.Success(ticket);
        }
    }

    private sealed class EscenarioDocente
    {
        public int AlumnoAjenoId { get; private init; }
        public int AsignacionAjenaId { get; private init; }
        public int EntregaAjenaId { get; private init; }
        public int EntregaPropiaId { get; private init; }
        public int PeriodoId { get; private init; }
        public int PlanificacionPropiaId { get; private init; }
        public int TareaPropiaId { get; private init; }

        public static async Task<EscenarioDocente> CrearAsync(IServiceProvider servicios)
        {
            var usuarios = servicios.GetRequiredService<UserManager<Usuario>>();
            var roles = servicios.GetRequiredService<RoleManager<Rol>>();
            var contexto = servicios.GetRequiredService<Contexto>();

            var rolDocente = await roles.FindByNameAsync(Roles.Docente);
            Assert.NotNull(rolDocente);
            var permisos = await roles.GetClaimsAsync(rolDocente!);
            Assert.Contains(permisos, x => x.Type == TiposClaims.Permiso && x.Value == Permisos.Tareas.Crear);
            Assert.Contains(permisos, x => x.Type == TiposClaims.Permiso && x.Value == Permisos.Calificaciones.Configurar);
            Assert.DoesNotContain(permisos, x => x.Type == TiposClaims.Permiso && x.Value == Permisos.Planificaciones.Revisar);
            Assert.DoesNotContain(permisos, x => x.Type == TiposClaims.Permiso && x.Value == Permisos.Calificaciones.AprobarCorreccion);

            await CrearUsuarioAsync(usuarios, UsuarioDocenteId, "docente.http", "Docente", "HTTP");
            await CrearUsuarioAsync(usuarios, "docente-ajeno-http", "docente.ajeno.http", "Docente", "Ajeno");
            await CrearUsuarioAsync(usuarios, "alumno-propio-http", "alumno.propio.http", "Alumno", "Propio");
            await CrearUsuarioAsync(usuarios, "alumno-ajeno-http", "alumno.ajeno.http", "Alumno", "Ajeno");
            var rolAsignado = await usuarios.AddToRoleAsync(
                (await usuarios.FindByIdAsync(UsuarioDocenteId))!, Roles.Docente);
            Assert.True(rolAsignado.Succeeded, string.Join("; ", rolAsignado.Errors.Select(x => x.Description)));

            var ciclo = new CicloEscolar
            {
                Anio = 2026,
                FechaInicio = new DateOnly(2026, 1, 1),
                FechaFin = new DateOnly(2026, 11, 30),
                Activo = true,
                Estado = EstadoCicloEscolar.Activo
            };
            var grado = new Grado
            {
                Nombre = "Quinto",
                Carrera = "Bachillerato en Computación",
                Nivel = NivelEducativo.Diversificado,
                Orden = 5
            };
            contexto.AddRange(ciclo, grado);
            await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

            var seccionPropia = new Seccion { CicloEscolarId = ciclo.Id, GradoId = grado.Id, Nombre = "A" };
            var seccionAjena = new Seccion { CicloEscolarId = ciclo.Id, GradoId = grado.Id, Nombre = "B" };
            var cursoPropio = new Curso { GradoId = grado.Id, Nombre = "Curso propio HTTP" };
            var cursoAjeno = new Curso { GradoId = grado.Id, Nombre = "Curso ajeno HTTP" };
            var docentePropio = new Docente { UsuarioId = UsuarioDocenteId, Carnet = "DOC-HTTP-1" };
            var docenteAjeno = new Docente { UsuarioId = "docente-ajeno-http", Carnet = "DOC-HTTP-2" };
            var alumnoPropio = new Alumno { UsuarioId = "alumno-propio-http", CodigoPersonal = "ALU-HTTP-1" };
            var alumnoAjeno = new Alumno { UsuarioId = "alumno-ajeno-http", CodigoPersonal = "ALU-HTTP-2" };
            var periodo = new Periodo
            {
                CicloEscolarId = ciclo.Id,
                Numero = 1,
                Nombre = "Primer bimestre",
                FechaInicio = new DateOnly(2026, 1, 1),
                FechaFin = new DateOnly(2026, 3, 15)
            };
            contexto.AddRange(seccionPropia, seccionAjena, cursoPropio, cursoAjeno,
                docentePropio, docenteAjeno, alumnoPropio, alumnoAjeno, periodo);
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
            contexto.AddRange(asignacionPropia, asignacionAjena);
            await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

            contexto.Inscripciones.AddRange(
                new Inscripcion
                {
                    AlumnoId = alumnoPropio.Id,
                    SeccionId = seccionPropia.Id,
                    CicloEscolarId = ciclo.Id,
                    Fecha = new DateOnly(2026, 1, 15)
                },
                new Inscripcion
                {
                    AlumnoId = alumnoAjeno.Id,
                    SeccionId = seccionAjena.Id,
                    CicloEscolarId = ciclo.Id,
                    Fecha = new DateOnly(2026, 1, 15)
                });
            var unidad = new Unidad
            {
                Titulo = "Unidad propia HTTP",
                CreadoPorUsuarioId = UsuarioDocenteId,
                FechaCreacion = DateTime.UtcNow
            };
            var planificacion = new Planificacion
            {
                GrupoVersionId = Guid.NewGuid(),
                DocenteId = docentePropio.Id,
                Tipo = TipoPlanificacion.PorUnidad,
                Titulo = "Planificación propia HTTP",
                Objetivos = "Objetivo de prueba",
                FechaCreacion = DateTime.UtcNow
            };
            var tareaPropia = new Tarea
            {
                AsignacionId = asignacionPropia.Id,
                Tipo = TipoTarea.Archivo,
                Titulo = "Tarea propia HTTP",
                Estado = EstadoTarea.Publicada,
                PunteoMaximo = 10,
                FechaDisponibilidad = DateTime.UtcNow.AddDays(-1),
                FechaLimite = DateTime.UtcNow.AddDays(3),
                FechaCreacion = DateTime.UtcNow,
                FechaPublicacion = DateTime.UtcNow,
                CreadoPorUsuarioId = UsuarioDocenteId,
                GrupoVersionId = Guid.NewGuid()
            };
            var tareaAjena = new Tarea
            {
                AsignacionId = asignacionAjena.Id,
                Tipo = TipoTarea.Archivo,
                Titulo = "Tarea ajena HTTP",
                Estado = EstadoTarea.Publicada,
                PunteoMaximo = 10,
                FechaDisponibilidad = DateTime.UtcNow.AddDays(-1),
                FechaLimite = DateTime.UtcNow.AddDays(3),
                FechaCreacion = DateTime.UtcNow,
                FechaPublicacion = DateTime.UtcNow,
                CreadoPorUsuarioId = "docente-ajeno-http",
                GrupoVersionId = Guid.NewGuid()
            };
            contexto.AddRange(unidad, planificacion, tareaPropia, tareaAjena);
            await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

            contexto.UnidadesAsignaciones.Add(new UnidadAsignacion
            {
                UnidadId = unidad.Id,
                AsignacionId = asignacionPropia.Id,
                PeriodoId = periodo.Id,
                Orden = 1,
                Estado = EstadoPublicacionUnidad.Publicada,
                FechaPublicacion = DateTime.UtcNow,
                AsignadoPorUsuarioId = UsuarioDocenteId
            });
            contexto.PlanificacionesAsignaciones.Add(new PlanificacionAsignacion
            {
                PlanificacionId = planificacion.Id,
                AsignacionId = asignacionPropia.Id,
                PeriodoId = periodo.Id,
                FechaInicio = new DateOnly(2026, 1, 1),
                FechaFin = new DateOnly(2026, 1, 7)
            });
            var inscripcionPropia = await contexto.Inscripciones.SingleAsync(
                x => x.AlumnoId == alumnoPropio.Id, TestContext.Current.CancellationToken);
            var inscripcionAjena = await contexto.Inscripciones.SingleAsync(
                x => x.AlumnoId == alumnoAjeno.Id, TestContext.Current.CancellationToken);
            var entregaPropia = new Entrega
            {
                TareaId = tareaPropia.Id,
                AlumnoId = alumnoPropio.Id,
                InscripcionId = inscripcionPropia.Id,
                Estado = EstadoEntrega.Enviada,
                FechaEntrega = DateTime.UtcNow,
                NumeroEnvio = 1
            };
            var entregaAjena = new Entrega
            {
                TareaId = tareaAjena.Id,
                AlumnoId = alumnoAjeno.Id,
                InscripcionId = inscripcionAjena.Id,
                Estado = EstadoEntrega.Enviada,
                FechaEntrega = DateTime.UtcNow,
                NumeroEnvio = 1
            };
            contexto.Entregas.AddRange(entregaPropia, entregaAjena);
            await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

            return new EscenarioDocente
            {
                AlumnoAjenoId = alumnoAjeno.Id,
                AsignacionAjenaId = asignacionAjena.Id,
                EntregaAjenaId = entregaAjena.Id,
                EntregaPropiaId = entregaPropia.Id,
                PeriodoId = periodo.Id,
                PlanificacionPropiaId = planificacion.Id,
                TareaPropiaId = tareaPropia.Id
            };
        }

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

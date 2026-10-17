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
public sealed class AlumnoHttpTests : IAsyncLifetime
{
    private const string ConfirmacionEsperada = "BASE_DESCARTABLE";
    private const string UsuarioAlumnoId = "alumno-http-prueba";
    private readonly string? conexionAdministrador =
        Environment.GetEnvironmentVariable("SABIA_PRUEBAS_PG");
    private readonly bool destinoConfirmado =
        Environment.GetEnvironmentVariable("SABIA_PRUEBAS_PG_CONFIRMAR") == ConfirmacionEsperada;
    private string? nombreBase;
    private string? conexionAplicacion;
    private string? directorioClaves;
    private FabricaSabiaAlumno? fabrica;
    private EscenarioAlumno escenario = null!;

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
        conexionAplicacion = constructor.ConnectionString;
        directorioClaves = Path.Combine(Path.GetTempPath(), "sabia-alumno-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directorioClaves);

        await using var administrador = new NpgsqlConnection(conexionAdministrador);
        await administrador.OpenAsync(TestContext.Current.CancellationToken);
        await using var comando = new NpgsqlCommand($"CREATE DATABASE \"{nombreBase}\"", administrador);
        await comando.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

        fabrica = new FabricaSabiaAlumno(conexionAplicacion, directorioClaves, UsuarioAlumnoId);
        using var ambito = fabrica.Services.CreateScope();
        escenario = await EscenarioAlumno.CrearAsync(ambito.ServiceProvider);
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

    private sealed class EscenarioAlumno
    {
        public int AlumnoAjenoId { get; private init; }
        public int AlumnoPropioId { get; private init; }
        public int AsignacionPropiaId { get; private init; }
        public int CuestionarioAjenoId { get; private init; }
        public int CuestionarioPropioId { get; private init; }
        public int EntregaAjenaId { get; private init; }
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
            await CrearUsuarioAsync(usuarios, "alumno-ajeno-http", "alumno.ajeno.http", "Alumno", "Ajeno");
            await CrearUsuarioAsync(usuarios, "docente-alumno-http", "docente.alumno.http", "Docente", "Alumno");
            await CrearUsuarioAsync(usuarios, "docente-ajeno-alumno-http", "docente.ajeno.alumno.http", "Docente", "Ajeno");
            var rolAsignado = await usuarios.AddToRoleAsync(
                (await usuarios.FindByIdAsync(UsuarioAlumnoId))!, Roles.Alumno);
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
                Nombre = "Sexto",
                Carrera = "Bachillerato en Computacion",
                Nivel = NivelEducativo.Diversificado,
                Orden = 6
            };
            contexto.AddRange(ciclo, grado);
            await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

            var seccionPropia = new Seccion { CicloEscolarId = ciclo.Id, GradoId = grado.Id, Nombre = "A" };
            var seccionAjena = new Seccion { CicloEscolarId = ciclo.Id, GradoId = grado.Id, Nombre = "B" };
            var cursoPropio = new Curso { GradoId = grado.Id, Nombre = "Curso alumno HTTP" };
            var cursoAjeno = new Curso { GradoId = grado.Id, Nombre = "Curso ajeno alumno HTTP" };
            var docentePropio = new Docente { UsuarioId = "docente-alumno-http", Carnet = "DOC-ALU-1" };
            var docenteAjeno = new Docente { UsuarioId = "docente-ajeno-alumno-http", Carnet = "DOC-ALU-2" };
            var alumnoPropio = new Alumno { UsuarioId = UsuarioAlumnoId, CodigoPersonal = "ALU-HTTP-1" };
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
            var unidad = new Unidad
            {
                Titulo = "Unidad alumno HTTP",
                CreadoPorUsuarioId = "docente-alumno-http",
                FechaCreacion = DateTime.UtcNow
            };
            contexto.AddRange(inscripcionPropia, inscripcionAjena, unidad);
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
            var tareaCuestionarioPropio = CrearTarea(asignacionPropia.Id, "Cuestionario propio HTTP", "docente-alumno-http", TipoTarea.Cuestionario);
            var tareaCuestionarioAjeno = CrearTarea(asignacionAjena.Id, "Cuestionario ajeno HTTP", "docente-ajeno-alumno-http", TipoTarea.Cuestionario);
            contexto.AddRange(tareaArchivo, tareaCuestionarioPropio, tareaCuestionarioAjeno);
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
            contexto.AddRange(cuestionarioPropio, cuestionarioAjeno, entregaPendiente, entregaAjena);
            await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

            return new EscenarioAlumno
            {
                AlumnoAjenoId = alumnoAjeno.Id,
                AlumnoPropioId = alumnoPropio.Id,
                AsignacionPropiaId = asignacionPropia.Id,
                CuestionarioAjenoId = cuestionarioAjeno.Id,
                CuestionarioPropioId = cuestionarioPropio.Id,
                EntregaAjenaId = entregaAjena.Id,
                EntregaPendienteId = entregaPendiente.Id,
                PeriodoId = periodo.Id
            };
        }

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

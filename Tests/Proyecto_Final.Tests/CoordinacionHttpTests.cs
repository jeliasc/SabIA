using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Seguridad.Filtros;

namespace Proyecto_Final.Tests;

[Collection("PostgreSQL temporal")]
public sealed class CoordinacionHttpTests : IAsyncLifetime
{
    private const string ConfirmacionEsperada = "BASE_DESCARTABLE";
    private readonly string? conexionAdministrador =
        Environment.GetEnvironmentVariable("SABIA_PRUEBAS_PG");
    private readonly bool destinoConfirmado =
        Environment.GetEnvironmentVariable("SABIA_PRUEBAS_PG_CONFIRMAR") == ConfirmacionEsperada;
    private string? nombreBase;
    private string? conexionAplicacion;
    private string? directorioClaves;
    private FabricaSabia? fabrica;

    public async ValueTask InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(conexionAdministrador) || !destinoConfirmado)
        {
            return;
        }

        nombreBase = "sabia_prueba_coordinacion_" + Guid.NewGuid().ToString("N")[..10];
        var constructor = new NpgsqlConnectionStringBuilder(conexionAdministrador)
        {
            Database = nombreBase,
            Pooling = false
        };
        conexionAplicacion = constructor.ConnectionString;
        directorioClaves = Path.Combine(Path.GetTempPath(), "sabia-coordinacion-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directorioClaves);

        await using var administrador = new NpgsqlConnection(conexionAdministrador);
        await administrador.OpenAsync(TestContext.Current.CancellationToken);
        await using var comando = new NpgsqlCommand($"CREATE DATABASE \"{nombreBase}\"", administrador);
        await comando.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

        fabrica = new FabricaSabia(conexionAplicacion, directorioClaves);
    }

    [Fact]
    public async Task Coordinacion_PuedeAbrirDashboardYAccionesAutorizadas()
    {
        OmitirSinPostgresDescartable();
        using var cliente = CrearCliente();

        var dashboard = await cliente.GetAsync("/?contexto=coordinacion", TestContext.Current.CancellationToken);
        var crearCiclo = await cliente.GetAsync("/Ciclos/Crear", TestContext.Current.CancellationToken);
        var revisarPlanificacion = await cliente.GetAsync("/Planificaciones/Revisar?id=999999", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
        Assert.Contains("Dashboard de coordinaci", await dashboard.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.OK, crearCiclo.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, revisarPlanificacion.StatusCode);
    }

    [Fact]
    public async Task Coordinacion_NoObtienePermisosNoAsignados_YLosPostExigenAntiforgery()
    {
        OmitirSinPostgresDescartable();
        using var cliente = CrearCliente(sinRedireccion: true);

        var calificaciones = await cliente.GetAsync(
            "/Calificaciones/Configurar?asignacionId=1",
            TestContext.Current.CancellationToken);
        var usuarios = await cliente.GetAsync("/Usuarios/Index", TestContext.Current.CancellationToken);
        var postSinAntiforgery = await cliente.PostAsync(
            "/Ciclos/Activar?id=1",
            content: null,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, calificaciones.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, usuarios.StatusCode);
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
                $"DROP DATABASE IF EXISTS \"{nombreBase}\" WITH (FORCE)",
                administrador);
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

    private sealed class FabricaSabia(string conexion, string directorioClaves)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration(configuracion =>
                configuracion.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:ConexionPrincipal"] = conexion,
                    ["SuperusuarioInicial:Usuario"] = "superusuario_http_prueba",
                    ["SuperusuarioInicial:Correo"] = "superusuario.http@example.test",
                    ["SuperusuarioInicial:Contrasena"] = $"A!a1{Guid.NewGuid():N}",
                    ["Correo:Habilitado"] = "false",
                    ["Logging:EventLog:LogLevel:Default"] = "None"
                }));
            builder.ConfigureServices(servicios =>
            {
                servicios.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(directorioClaves));
                servicios.PostConfigure<MvcOptions>(opciones =>
                {
                    for (var indice = opciones.Filters.Count - 1; indice >= 0; indice--)
                    {
                        if (opciones.Filters[indice] is ServiceFilterAttribute
                            {
                                ServiceType: var tipoServicio
                            } && tipoServicio == typeof(ForzarCambioContrasenaFiltro))
                        {
                            opciones.Filters.RemoveAt(indice);
                        }
                    }
                });
                servicios.AddAuthentication(opciones =>
                {
                    opciones.DefaultAuthenticateScheme = AutenticacionCoordinacion.Esquema;
                    opciones.DefaultChallengeScheme = AutenticacionCoordinacion.Esquema;
                    opciones.DefaultForbidScheme = AutenticacionCoordinacion.Esquema;
                }).AddScheme<AuthenticationSchemeOptions, AutenticacionCoordinacion>(
                    AutenticacionCoordinacion.Esquema,
                    _ => { });
            });
        }
    }

    private sealed class AutenticacionCoordinacion(
        IOptionsMonitor<AuthenticationSchemeOptions> opciones,
        ILoggerFactory logger,
        UrlEncoder codificador)
        : AuthenticationHandler<AuthenticationSchemeOptions>(opciones, logger, codificador)
    {
        public const string Esquema = "CoordinacionPrueba";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var permisos = new[]
            {
                Permisos.Dashboard.CoordinacionVer,
                Permisos.Ciclos.Ver,
                Permisos.Ciclos.Crear,
                Permisos.Ciclos.Activar,
                Permisos.Planificaciones.Ver,
                Permisos.Planificaciones.Revisar,
                Permisos.Docentes.Ver,
                Permisos.Alumnos.Ver,
                Permisos.Asignaciones.Ver,
                Permisos.Calificaciones.Ver,
                Permisos.Tareas.Ver,
                Permisos.Notificaciones.Ver
            };
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "coordinacion-http"),
                new(ClaimTypes.Name, "coordinacion.http")
            };
            claims.AddRange(permisos.Select(permiso => new Claim(TiposClaims.Permiso, permiso)));
            var identidad = new ClaimsIdentity(claims, Esquema);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identidad), Esquema);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}

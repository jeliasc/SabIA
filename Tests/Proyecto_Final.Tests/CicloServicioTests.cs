using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.Auditoria;
using Proyecto_Final.Servicios.GestionCiclos;
using Proyecto_Final.ViewModels.Ciclos;

namespace Proyecto_Final.Tests;

public sealed class CicloServicioTests
{
    [Fact]
    public async Task Activar_SinOtroActivo_RegistraMovimientoYAuditoria()
    {
        using var proveedor = CrearProveedor(out var accesor);
        using var alcance = proveedor.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<Contexto>();
        await SembrarActorYCicloAsync(contexto, EstadoCicloEscolar.Preparacion);
        Autenticar(accesor, Permisos.Ciclos.Activar);

        var resultado = await Servicio(alcance).ActivarAsync(1);

        Assert.True(resultado.Exitoso);
        var ciclo = await contexto.CiclosEscolares.SingleAsync(TestContext.Current.CancellationToken);
        Assert.True(ciclo.Activo);
        Assert.Equal(EstadoCicloEscolar.Activo, ciclo.Estado);
        Assert.Single(contexto.MovimientosCiclosEscolares);
        Assert.Single(contexto.RegistrosAuditoria);
    }

    [Fact]
    public async Task Activar_ConOtroActivo_RechazaSinReemplazarlo()
    {
        using var proveedor = CrearProveedor(out var accesor);
        using var alcance = proveedor.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<Contexto>();
        await SembrarActorYCicloAsync(contexto, EstadoCicloEscolar.Preparacion);
        contexto.CiclosEscolares.Add(Ciclo(2, 2025, EstadoCicloEscolar.Activo));
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        Autenticar(accesor, Permisos.Ciclos.Activar);

        var resultado = await Servicio(alcance).ActivarAsync(1);

        Assert.False(resultado.Exitoso);
        Assert.Contains("2025", resultado.Mensaje);
        Assert.Equal(EstadoCicloEscolar.Preparacion,
            (await contexto.CiclosEscolares.FindAsync([1], TestContext.Current.CancellationToken))!.Estado);
        Assert.True((await contexto.CiclosEscolares.FindAsync([2], TestContext.Current.CancellationToken))!.Activo);
        Assert.Empty(contexto.MovimientosCiclosEscolares);
    }

    [Fact]
    public async Task Activar_NoReactivaCicloCerrado()
    {
        using var proveedor = CrearProveedor(out var accesor);
        using var alcance = proveedor.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<Contexto>();
        await SembrarActorYCicloAsync(contexto, EstadoCicloEscolar.Cerrado);
        Autenticar(accesor, Permisos.Ciclos.Activar);

        var resultado = await Servicio(alcance).ActivarAsync(1);

        Assert.False(resultado.Exitoso);
        Assert.Contains("reapertura excepcional", resultado.Mensaje);
    }

    [Fact]
    public void ConcurrenciaDeActivaciones_EstaRespaldadaPorIndiceUnicoFiltrado()
    {
        using var proveedor = CrearProveedor(out _);
        using var alcance = proveedor.CreateScope();
        var entidad = alcance.ServiceProvider.GetRequiredService<Contexto>()
            .Model.FindEntityType(typeof(CicloEscolar))!;
        var indice = Assert.Single(entidad.GetIndexes(), x =>
            x.Properties.Select(p => p.Name).SequenceEqual([nameof(CicloEscolar.Activo)]));

        Assert.True(indice.IsUnique);
        Assert.Equal("\"Activo\" = TRUE", indice.GetFilter());
    }

    [Fact]
    public async Task Cerrar_SinObligacionesPendientes_CierraFormalmente()
    {
        using var proveedor = CrearProveedor(out var accesor);
        using var alcance = proveedor.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<Contexto>();
        await SembrarActorYCicloAsync(contexto, EstadoCicloEscolar.Activo);
        AutenticarCoordinacion(accesor, Permisos.Ciclos.Cerrar);

        var resultado = await Servicio(alcance).CerrarAsync(1);

        Assert.True(resultado.Exitoso);
        var ciclo = await contexto.CiclosEscolares.SingleAsync(TestContext.Current.CancellationToken);
        Assert.False(ciclo.Activo);
        Assert.Equal(EstadoCicloEscolar.Cerrado, ciclo.Estado);
        Assert.Single(contexto.MovimientosCiclosEscolares, x => x.Tipo == TipoMovimientoCicloEscolar.Cierre);
        Assert.Single(contexto.RegistrosAuditoria, x => x.Accion == "Cerrar");
    }

    [Fact]
    public async Task Cerrar_AsignacionSinAlumnos_NoGeneraBloqueoInjustificado()
    {
        using var proveedor = CrearProveedor(out var accesor);
        using var alcance = proveedor.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<Contexto>();
        await SembrarEscenarioEvaluableAsync(contexto, incluirCierreCompleto: false,
            incluirCorreccion: false, incluirInscripcion: false);
        AutenticarCoordinacion(accesor, Permisos.Ciclos.Cerrar);

        var revision = await Servicio(alcance).ObtenerRevisionCierreAsync(1);
        var resultado = await Servicio(alcance).CerrarAsync(1);

        Assert.NotNull(revision);
        Assert.Equal(0, revision.AsignacionesEvaluables);
        Assert.Equal(1, revision.AsignacionesSinObligaciones);
        Assert.Empty(revision.Pendientes);
        Assert.True(resultado.Exitoso);
    }

    [Fact]
    public async Task Cerrar_ConCalificacionesPendientes_RechazaYNoAuditaExito()
    {
        using var proveedor = CrearProveedor(out var accesor);
        using var alcance = proveedor.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<Contexto>();
        await SembrarEscenarioEvaluableAsync(contexto, incluirCierreCompleto: false, incluirCorreccion: false);
        AutenticarCoordinacion(accesor, Permisos.Ciclos.Cerrar);

        var revision = await Servicio(alcance).ObtenerRevisionCierreAsync(1);
        var resultado = await Servicio(alcance).CerrarAsync(1);

        Assert.NotNull(revision);
        Assert.Contains(revision.Pendientes, x => x.Motivo.Contains("no han sido cerradas"));
        Assert.False(resultado.Exitoso);
        Assert.Empty(contexto.MovimientosCiclosEscolares);
        Assert.Empty(contexto.RegistrosAuditoria);
        Assert.True((await contexto.CiclosEscolares.FindAsync([1], TestContext.Current.CancellationToken))!.Activo);
    }

    [Fact]
    public async Task Cerrar_ConCorreccionAprobadaSinAplicar_Rechaza()
    {
        using var proveedor = CrearProveedor(out var accesor);
        using var alcance = proveedor.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<Contexto>();
        await SembrarEscenarioEvaluableAsync(contexto, incluirCierreCompleto: true, incluirCorreccion: true);
        AutenticarCoordinacion(accesor, Permisos.Ciclos.Cerrar);

        var revision = await Servicio(alcance).ObtenerRevisionCierreAsync(1);

        Assert.NotNull(revision);
        Assert.Contains(revision.Pendientes, x => x.Motivo.Contains("correcciones"));
        Assert.False((await Servicio(alcance).CerrarAsync(1)).Exitoso);
    }

    [Fact]
    public async Task Reabrir_Excepcional_ConservaCierresYRegistraJustificacion()
    {
        using var proveedor = CrearProveedor(out var accesor);
        using var alcance = proveedor.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<Contexto>();
        await SembrarEscenarioEvaluableAsync(contexto, incluirCierreCompleto: true, incluirCorreccion: false,
            estadoCiclo: EstadoCicloEscolar.Cerrado);
        AutenticarCoordinacion(accesor, Permisos.Ciclos.Reabrir);

        var resultado = await Servicio(alcance).ReabrirAsync(new ReabrirCiclo
        {
            Id = 1,
            Justificacion = "Corrección institucional autorizada."
        });

        Assert.True(resultado.Exitoso);
        Assert.Equal(EstadoCierreCalificaciones.Cerrado,
            (await contexto.CierresCalificaciones.SingleAsync(TestContext.Current.CancellationToken)).Estado);
        var movimiento = await contexto.MovimientosCiclosEscolares.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(TipoMovimientoCicloEscolar.Reapertura, movimiento.Tipo);
        Assert.Equal("Corrección institucional autorizada.", movimiento.Justificacion);
        Assert.Equal("Corrección institucional autorizada.",
            (await contexto.RegistrosAuditoria.SingleAsync(TestContext.Current.CancellationToken)).Justificacion);
    }

    [Fact]
    public async Task Reabrir_ConOtroCicloActivo_Rechaza()
    {
        using var proveedor = CrearProveedor(out var accesor);
        using var alcance = proveedor.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<Contexto>();
        await SembrarActorYCicloAsync(contexto, EstadoCicloEscolar.Cerrado);
        contexto.CiclosEscolares.Add(Ciclo(2, 2027, EstadoCicloEscolar.Activo));
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        AutenticarCoordinacion(accesor, Permisos.Ciclos.Reabrir);

        var resultado = await Servicio(alcance).ReabrirAsync(new ReabrirCiclo
        {
            Id = 1,
            Justificacion = "Motivo institucional suficiente."
        });

        Assert.False(resultado.Exitoso);
        Assert.Contains("2027", resultado.Mensaje);
        Assert.Empty(contexto.MovimientosCiclosEscolares);
    }

    [Fact]
    public async Task Reabrir_ExigeJustificacion()
    {
        using var proveedor = CrearProveedor(out var accesor);
        using var alcance = proveedor.CreateScope();
        await SembrarActorYCicloAsync(alcance.ServiceProvider.GetRequiredService<Contexto>(), EstadoCicloEscolar.Cerrado);
        AutenticarCoordinacion(accesor, Permisos.Ciclos.Reabrir);

        var resultado = await Servicio(alcance).ReabrirAsync(new ReabrirCiclo { Id = 1, Justificacion = "Breve" });

        Assert.False(resultado.Exitoso);
        Assert.Contains(nameof(ReabrirCiclo.Justificacion), resultado.Errores.Keys);
    }

    [Fact]
    public async Task PermisoCierre_SinPerfilCoordinacion_EsRechazadoEnServicio()
    {
        using var proveedor = CrearProveedor(out var accesor);
        using var alcance = proveedor.CreateScope();
        await SembrarActorYCicloAsync(alcance.ServiceProvider.GetRequiredService<Contexto>(), EstadoCicloEscolar.Activo);
        Autenticar(accesor, Permisos.Ciclos.Cerrar);

        var resultado = await Servicio(alcance).CerrarAsync(1);

        Assert.False(resultado.Exitoso);
        Assert.Contains("permiso", resultado.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PermisoReapertura_SinPerfilCoordinacion_EsRechazadoEnServicio()
    {
        using var proveedor = CrearProveedor(out var accesor);
        using var alcance = proveedor.CreateScope();
        await SembrarActorYCicloAsync(alcance.ServiceProvider.GetRequiredService<Contexto>(), EstadoCicloEscolar.Cerrado);
        Autenticar(accesor, Permisos.Ciclos.Reabrir);

        var resultado = await Servicio(alcance).ReabrirAsync(new ReabrirCiclo
        {
            Id = 1,
            Justificacion = "Motivo institucional suficiente."
        });

        Assert.False(resultado.Exitoso);
        Assert.Contains("permiso", resultado.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Periodo_EnCicloCerrado_NoPuedeModificarse_PeroSeConservaParaLectura()
    {
        using var proveedor = CrearProveedor(out _);
        using var alcance = proveedor.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<Contexto>();
        await SembrarActorYCicloAsync(contexto, EstadoCicloEscolar.Cerrado);
        contexto.Periodos.Add(new Periodo
        {
            Id = 1, CicloEscolarId = 1, Numero = 1, Nombre = "Primero",
            FechaInicio = new DateOnly(2026, 1, 1), FechaFin = new DateOnly(2026, 3, 1)
        });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        var servicio = new Servicios.GestionPeriodos.PeriodoServicio(contexto);

        var listado = await servicio.ObtenerTodosAsync();
        var resultado = await servicio.EditarAsync(new ViewModels.Periodos.EditarPeriodo
        {
            Id = 1, CicloEscolarId = 1, Numero = 1, Nombre = "Modificado",
            FechaInicio = new DateOnly(2026, 1, 1), FechaFin = new DateOnly(2026, 3, 1)
        });

        Assert.Single(listado);
        Assert.False(resultado.Exitoso);
        Assert.Contains("cerrado", resultado.Errores.Values.Single(), StringComparison.OrdinalIgnoreCase);
    }

    private static ServiceProvider CrearProveedor(out HttpContextAccessor accesor)
    {
        accesor = new HttpContextAccessor();
        var nombre = Guid.NewGuid().ToString();
        var servicios = new ServiceCollection();
        servicios.AddSingleton<IHttpContextAccessor>(accesor);
        servicios.AddDbContext<Contexto>(opciones => opciones
            .UseInMemoryDatabase(nombre)
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        servicios.AddScoped<IAuditoriaServicio, AuditoriaServicio>();
        servicios.AddScoped<ICicloServicio, CicloServicio>();
        return servicios.BuildServiceProvider();
    }

    private static ICicloServicio Servicio(IServiceScope alcance) =>
        alcance.ServiceProvider.GetRequiredService<ICicloServicio>();

    private static void Autenticar(HttpContextAccessor accesor, params string[] permisos)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "actor") };
        claims.AddRange(permisos.Select(x => new Claim(TiposClaims.Permiso, x)));
        accesor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Prueba"))
        };
    }

    private static void AutenticarCoordinacion(HttpContextAccessor accesor, string permiso) =>
        Autenticar(accesor, permiso, Permisos.Planificaciones.Revisar);

    private static async Task SembrarActorYCicloAsync(Contexto contexto, EstadoCicloEscolar estado)
    {
        contexto.Users.Add(Usuario("actor", "Responsable"));
        contexto.CiclosEscolares.Add(Ciclo(1, 2026, estado));
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task SembrarEscenarioEvaluableAsync(
        Contexto contexto,
        bool incluirCierreCompleto,
        bool incluirCorreccion,
        EstadoCicloEscolar estadoCiclo = EstadoCicloEscolar.Activo,
        bool incluirInscripcion = true)
    {
        var actor = Usuario("actor", "Responsable");
        var usuarioDocente = Usuario("docente", "Docente");
        var usuarioAlumno = Usuario("alumno", "Alumno");
        var ciclo = Ciclo(1, 2026, estadoCiclo);
        var periodo = new Periodo
        {
            Id = 1, CicloEscolar = ciclo, Numero = 1, Nombre = "Primero",
            FechaInicio = new DateOnly(2026, 1, 1), FechaFin = new DateOnly(2026, 3, 1)
        };
        var grado = new Grado { Id = 1, Nombre = "Primero", Carrera = "Básico", Nivel = NivelEducativo.Basico, Orden = 1 };
        var seccion = new Seccion { Id = 1, Nombre = "A", CicloEscolar = ciclo, Grado = grado, Estado = EstadoRegistro.Activo };
        var curso = new Curso { Id = 1, Nombre = "Matemática", Grado = grado, Estado = EstadoRegistro.Activo };
        var docente = new Docente { Id = 1, Usuario = usuarioDocente, Carnet = "D-1" };
        var asignacion = new Asignacion { Id = 1, Seccion = seccion, Curso = curso, GradoId = 1, Docente = docente, Estado = EstadoRegistro.Activo };
        var alumno = new Alumno { Id = 1, Usuario = usuarioAlumno, CodigoPersonal = "A-1" };
        var inscripcion = new Inscripcion
        {
            Id = 1, Alumno = alumno, Seccion = seccion, CicloEscolarId = 1,
            Fecha = new DateOnly(2026, 1, 10), Estado = EstadoInscripcion.Activa
        };
        var configuracion = new ConfiguracionEvaluacion
        {
            Id = 1, Asignacion = asignacion, SeccionId = 1, CicloEscolarId = 1,
            Periodo = periodo, Estado = EstadoConfiguracionEvaluacion.Activa,
            FechaCreacion = DateTime.UtcNow, ConfiguradoPorUsuario = actor
        };
        var actividad = new ActividadEvaluable
        {
            Id = 1, ConfiguracionEvaluacion = configuracion, AsignacionId = 1,
            Origen = OrigenActividadEvaluable.Manual, Nombre = "Actividad", PunteoMaximo = 100,
            Orden = 1, Activa = true, FechaCreacion = DateTime.UtcNow
        };
        contexto.AddRange(actor, usuarioDocente, ciclo, periodo, grado, seccion,
            curso, docente, asignacion, configuracion, actividad);
        if (incluirInscripcion)
            contexto.AddRange(usuarioAlumno, alumno, inscripcion);

        if (incluirCierreCompleto)
        {
            var cierre = new CierreCalificaciones
            {
                Id = 1, ConfiguracionEvaluacion = configuracion,
                Estado = EstadoCierreCalificaciones.Cerrado,
                FechaCierre = DateTime.UtcNow, CerradoPorUsuario = actor
            };
            var resultado = new ResultadoCalificacionPeriodo
            {
                Id = 1, ConfiguracionEvaluacion = configuracion,
                Inscripcion = inscripcion, Alumno = alumno, FechaCierre = DateTime.UtcNow
            };
            contexto.AddRange(cierre, resultado);
            if (incluirCorreccion)
            {
                contexto.SolicitudesCorreccionesCalificaciones.Add(new SolicitudCorreccionCalificacion
                {
                    Id = 1, ResultadoCalificacionPeriodo = resultado,
                    ActividadEvaluable = actividad, Inscripcion = inscripcion, Alumno = alumno,
                    MotivoSolicitud = "Corrección", Estado = EstadoSolicitudCorreccion.Aprobada,
                    SolicitadaPorUsuario = actor, FechaSolicitud = DateTime.UtcNow
                });
            }
        }

        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static CicloEscolar Ciclo(int id, int anio, EstadoCicloEscolar estado) => new()
    {
        Id = id,
        Anio = anio,
        FechaInicio = new DateOnly(anio, 1, 1),
        FechaFin = new DateOnly(anio, 11, 30),
        Estado = estado,
        Activo = estado == EstadoCicloEscolar.Activo
    };

    private static Usuario Usuario(string id, string nombre) => new()
    {
        Id = id,
        UserName = id,
        PrimerNombre = nombre,
        PrimerApellido = "Prueba",
        Activo = true
    };
}

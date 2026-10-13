using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.GestionCalificaciones;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Calificaciones;

namespace Proyecto_Final.Tests;

public sealed class FlujoCalificacionesTests
{
    [Fact]
    public async Task Cierre_CongelaResultadoYBloqueaEdicionOrdinaria()
    {
        await using var contexto = CrearContexto();
        await SembrarLibroCompletoAsync(contexto);
        var servicio = CrearServicioDocente(contexto);

        var cierre = await servicio.CerrarAsync(1);

        Assert.True(cierre.Exitoso);
        var resultado = await contexto.ResultadosCalificacionesPeriodos.SingleAsync(
            TestContext.Current.CancellationToken);
        Assert.Equal(79m, resultado.NotaBimestral);

        var registro = await contexto.CalificacionesManuales.FirstAsync(
            TestContext.Current.CancellationToken);
        registro.Nota = 0;
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var libroHistorico = await servicio.ObtenerLibroAsync(1, false, false);
        Assert.NotNull(libroHistorico);
        Assert.Equal(79m, libroHistorico!.Alumnos.Single().NotaBimestral);

        var edicion = await servicio.GuardarLibroAsync(new GuardarLibroCalificaciones
        {
            ConfiguracionId = 1,
            Notas =
            [
                new NotaLibroCalificacion
                {
                    ActividadId = 1,
                    InscripcionId = 1,
                    Nota = 7
                }
            ]
        });
        Assert.False(edicion.Exitoso);
    }

    [Fact]
    public async Task Correccion_AprobadaSeAplicaUnaVezYActualizaSnapshotAuditado()
    {
        await using var contexto = CrearContexto();
        await SembrarLibroCompletoAsync(contexto);
        var docente = CrearServicioDocente(contexto);
        Assert.True((await docente.CerrarAsync(1)).Exitoso);

        var resultado = await contexto.ResultadosCalificacionesPeriodos.SingleAsync(
            TestContext.Current.CancellationToken);
        var solicitud = await docente.SolicitarCorreccionAsync(
            new SolicitarCorreccionCalificacion
            {
                ConfiguracionId = 1,
                ResultadoId = resultado.Id,
                ActividadId = 1,
                NotaPropuesta = 7,
                Motivo = "Corrección documentada de digitación."
            });
        Assert.True(solicitud.Exitoso);

        var registroSolicitud = await contexto.SolicitudesCorreccionesCalificaciones
            .SingleAsync(TestContext.Current.CancellationToken);
        var tokenPendiente = registroSolicitud.VersionConcurrencia;
        var coordinacion = CrearServicio(
            contexto,
            "coordinacion",
            Permisos.Calificaciones.Ver,
            Permisos.Calificaciones.AprobarCorreccion);
        var revision = await coordinacion.RevisarCorreccionAsync(
            new RevisarCorreccionCalificacion
            {
                Id = registroSolicitud.Id,
                Aprobar = true,
                VersionConcurrencia = tokenPendiente
            });
        Assert.True(revision.Exitoso);

        var revisionRepetida = await coordinacion.RevisarCorreccionAsync(
            new RevisarCorreccionCalificacion
            {
                Id = registroSolicitud.Id,
                Aprobar = true,
                VersionConcurrencia = tokenPendiente
            });
        Assert.False(revisionRepetida.Exitoso);

        registroSolicitud = await contexto.SolicitudesCorreccionesCalificaciones
            .SingleAsync(TestContext.Current.CancellationToken);
        docente = CrearServicioDocente(contexto);
        var aplicacion = await docente.AplicarCorreccionAsync(
            new AplicarCorreccionCalificacion
            {
                Id = registroSolicitud.Id,
                VersionConcurrencia = registroSolicitud.VersionConcurrencia
            });
        Assert.True(aplicacion.Exitoso, aplicacion.Mensaje);

        var tokenUsado = registroSolicitud.VersionConcurrencia;
        var segundaAplicacion = await docente.AplicarCorreccionAsync(
            new AplicarCorreccionCalificacion
            {
                Id = registroSolicitud.Id,
                VersionConcurrencia = tokenUsado
            });
        Assert.False(segundaAplicacion.Exitoso);

        Assert.Equal(EstadoSolicitudCorreccion.Aplicada, registroSolicitud.Estado);
        Assert.Equal(78m, resultado.NotaBimestral);
        Assert.Single(contexto.HistorialesCalificaciones);
    }

    private static Contexto CrearContexto() => new(
        new DbContextOptionsBuilder<Contexto>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private static CalificacionServicio CrearServicioDocente(Contexto contexto) =>
        CrearServicio(
            contexto,
            "docente",
            Permisos.Calificaciones.Ver,
            Permisos.Calificaciones.Registrar,
            Permisos.Calificaciones.Cerrar,
            Permisos.Calificaciones.SolicitarCorreccion);

    private static CalificacionServicio CrearServicio(
        Contexto contexto,
        string usuarioId,
        params string[] permisos)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, usuarioId) };
        claims.AddRange(permisos.Select(x => new Claim(TiposClaims.Permiso, x)));
        var accesor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Prueba"))
            }
        };
        return new CalificacionServicio(
            contexto,
            new AccesoAcademicoServicio(contexto, accesor));
    }

    private static async Task SembrarLibroCompletoAsync(Contexto contexto)
    {
        var usuarioDocente = Usuario("docente");
        var usuarioAlumno = Usuario("alumno");
        var usuarioCoordinacion = Usuario("coordinacion");
        var ciclo = new CicloEscolar
        {
            Id = 1,
            Anio = 2026,
            FechaInicio = new DateOnly(2026, 1, 1),
            FechaFin = new DateOnly(2026, 12, 1),
            Activo = true
        };
        var periodo = new Periodo
        {
            Id = 1,
            CicloEscolar = ciclo,
            Numero = 1,
            Nombre = "Primer bimestre",
            FechaInicio = new DateOnly(2026, 1, 1),
            FechaFin = new DateOnly(2026, 3, 1)
        };
        var grado = new Grado
        {
            Id = 1,
            Nivel = NivelEducativo.Diversificado,
            Nombre = "Quinto",
            Carrera = "Bachillerato",
            Orden = 1
        };
        var seccion = new Seccion
        {
            Id = 1,
            Nombre = "A",
            CicloEscolar = ciclo,
            Grado = grado,
            Estado = EstadoRegistro.Activo
        };
        var curso = new Curso { Id = 1, Nombre = "Ciencias", Grado = grado };
        var docente = new Docente { Id = 1, Usuario = usuarioDocente, Carnet = "D-1" };
        var alumno = new Alumno { Id = 1, Usuario = usuarioAlumno, CodigoPersonal = "A-1" };
        var asignacion = new Asignacion
        {
            Id = 1,
            Seccion = seccion,
            Curso = curso,
            GradoId = grado.Id,
            Docente = docente,
            Estado = EstadoRegistro.Activo
        };
        var inscripcion = new Inscripcion
        {
            Id = 1,
            Alumno = alumno,
            Seccion = seccion,
            CicloEscolarId = ciclo.Id,
            Fecha = new DateOnly(2026, 1, 10),
            Estado = EstadoInscripcion.Activa
        };
        var configuracion = new ConfiguracionEvaluacion
        {
            Id = 1,
            Asignacion = asignacion,
            SeccionId = seccion.Id,
            CicloEscolarId = ciclo.Id,
            Periodo = periodo,
            MetodoCalculo = MetodoCalculoEvaluacion.SumaPuntos,
            Estado = EstadoConfiguracionEvaluacion.Activa,
            FechaCreacion = DateTime.UtcNow,
            ConfiguradoPorUsuario = usuarioDocente
        };
        var desempeno = new CategoriaEvaluacion
        {
            Id = 1,
            ConfiguracionEvaluacion = configuracion,
            Nombre = "Desempeño",
            Tipo = TipoCategoriaEvaluacion.Desempeno,
            Porcentaje = 80,
            Orden = 1
        };
        var actitudinal = new CategoriaEvaluacion
        {
            Id = 2,
            ConfiguracionEvaluacion = configuracion,
            Nombre = "Actitudinal",
            Tipo = TipoCategoriaEvaluacion.Actitudinal,
            Porcentaje = 20,
            Orden = 2
        };
        var actividades = new List<ActividadEvaluable>();
        for (var i = 1; i <= 8; i++)
        {
            actividades.Add(new ActividadEvaluable
            {
                Id = i,
                ConfiguracionEvaluacion = configuracion,
                AsignacionId = asignacion.Id,
                CategoriaEvaluacion = desempeno,
                Origen = OrigenActividadEvaluable.Manual,
                Nombre = $"D{i}",
                PunteoMaximo = 10,
                Orden = i,
                FechaCreacion = DateTime.UtcNow
            });
        }
        for (var i = 1; i <= 5; i++)
        {
            actividades.Add(new ActividadEvaluable
            {
                Id = i + 8,
                ConfiguracionEvaluacion = configuracion,
                AsignacionId = asignacion.Id,
                CategoriaEvaluacion = actitudinal,
                Origen = OrigenActividadEvaluable.Manual,
                Nombre = $"A{i}",
                PunteoMaximo = 4,
                Orden = i + 8,
                FechaCreacion = DateTime.UtcNow
            });
        }
        contexto.AddRange(
            usuarioDocente, usuarioAlumno, usuarioCoordinacion,
            ciclo, periodo, grado, seccion, curso, docente, alumno,
            asignacion, inscripcion, configuracion, desempeno, actitudinal);
        contexto.AddRange(actividades);
        contexto.AddRange(actividades.Select(a => new CalificacionManual
        {
            ActividadEvaluable = a,
            Alumno = alumno,
            Inscripcion = inscripcion,
            Estado = EstadoCalificacionManual.Calificada,
            Nota = a.CategoriaEvaluacion == desempeno ? 8 : 3,
            FechaCalificacion = DateTime.UtcNow,
            CalificadoPorUsuario = usuarioDocente
        }));
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static Usuario Usuario(string id) => new()
    {
        Id = id,
        UserName = id,
        NormalizedUserName = id.ToUpperInvariant(),
        PrimerNombre = id,
        PrimerApellido = "Prueba",
        Activo = true
    };
}

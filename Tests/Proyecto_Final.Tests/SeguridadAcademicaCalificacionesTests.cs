using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.SeguridadAcademica;

namespace Proyecto_Final.Tests;

public sealed class SeguridadAcademicaCalificacionesTests
{
    [Fact]
    public async Task Docente_SoloEsResponsableDeSuAsignacionVigente()
    {
        await using var contexto = CrearContexto();
        await SembrarAsync(contexto);
        var servicio = CrearServicio(contexto, "docente-1");

        Assert.True(await servicio.EsDocenteDeAsignacionAsync(1));
        Assert.False(await servicio.EsDocenteDeAsignacionAsync(2));
    }

    [Fact]
    public async Task Alumno_SoloPerteneceALaAsignacionDeSuInscripcionActiva()
    {
        await using var contexto = CrearContexto();
        await SembrarAsync(contexto);
        var servicio = CrearServicio(contexto, "alumno-1");

        Assert.True(await servicio.AlumnoPerteneceAsignacionAsync(1));
        Assert.False(await servicio.AlumnoPerteneceAsignacionAsync(2));
    }

    [Fact]
    public async Task InscripcionRetirada_NoAutorizaContenidoVigente()
    {
        await using var contexto = CrearContexto();
        await SembrarAsync(contexto);
        contexto.Inscripciones.Single().Estado = EstadoInscripcion.Retirada;
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        var servicio = CrearServicio(contexto, "alumno-1");

        Assert.False(await servicio.AlumnoPerteneceAsignacionAsync(1));
    }

    [Fact]
    public void PermisoExplicito_SeEvaluaSinInferirloDelPerfil()
    {
        using var contexto = CrearContexto();
        var servicio = CrearServicio(
            contexto,
            "operador",
            Permisos.Calificaciones.AprobarCorreccion);

        Assert.True(servicio.TienePermiso(Permisos.Calificaciones.AprobarCorreccion));
        Assert.False(servicio.TienePermiso(Permisos.Calificaciones.Registrar));
    }

    private static Contexto CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<Contexto>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new Contexto(opciones);
    }

    private static AccesoAcademicoServicio CrearServicio(
        Contexto contexto,
        string usuarioId,
        params string[] permisos)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuarioId)
        };
        claims.AddRange(permisos.Select(x => new Claim(TiposClaims.Permiso, x)));
        var accesor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Prueba"))
            }
        };
        return new AccesoAcademicoServicio(contexto, accesor);
    }

    private static async Task SembrarAsync(Contexto contexto)
    {
        var ciclo = new CicloEscolar
        {
            Id = 1,
            Anio = 2026,
            FechaInicio = new DateOnly(2026, 1, 1),
            FechaFin = new DateOnly(2026, 12, 1),
            Activo = true
        };
        var grado = new Grado
        {
            Id = 1,
            Nivel = NivelEducativo.Diversificado,
            Nombre = "Quinto",
            Carrera = "Bachillerato",
            Orden = 1
        };
        var seccion1 = new Seccion { Id = 1, Nombre = "A", CicloEscolar = ciclo, Grado = grado };
        var seccion2 = new Seccion { Id = 2, Nombre = "B", CicloEscolar = ciclo, Grado = grado };
        var usuarioDocente1 = Usuario("docente-1");
        var usuarioDocente2 = Usuario("docente-2");
        var usuarioAlumno = Usuario("alumno-1");
        var docente1 = new Docente { Id = 1, Usuario = usuarioDocente1, Carnet = "D1" };
        var docente2 = new Docente { Id = 2, Usuario = usuarioDocente2, Carnet = "D2" };
        var alumno = new Alumno { Id = 1, Usuario = usuarioAlumno, CodigoPersonal = "A1" };
        var curso = new Curso { Id = 1, Nombre = "Ciencias", Grado = grado };
        contexto.AddRange(
            ciclo, grado, seccion1, seccion2,
            usuarioDocente1, usuarioDocente2, usuarioAlumno,
            docente1, docente2, alumno, curso,
            new Asignacion
            {
                Id = 1, Seccion = seccion1, Curso = curso, GradoId = grado.Id,
                Docente = docente1, Estado = EstadoRegistro.Activo
            },
            new Asignacion
            {
                Id = 2, Seccion = seccion2, Curso = curso, GradoId = grado.Id,
                Docente = docente2, Estado = EstadoRegistro.Activo
            },
            new Inscripcion
            {
                Id = 1, Alumno = alumno, Seccion = seccion1, CicloEscolarId = ciclo.Id,
                Fecha = new DateOnly(2026, 1, 10), Estado = EstadoInscripcion.Activa
            });
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

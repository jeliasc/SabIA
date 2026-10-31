using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.GestionCiclos;
using Proyecto_Final.ViewModels.ExploradorAcademico;

namespace Proyecto_Final.Servicios.ExploradorAcademico;

/// <summary>Lecturas jerárquicas: no altera la estructura ni sustituye módulos existentes.</summary>
public sealed class ExploradorAcademicoServicio(
    Contexto contexto,
    IContextoCicloEscolar contextoCiclo) : IExploradorAcademicoServicio
{
    public async Task<ExploradorAcademicoIndice?> ObtenerIndiceAsync()
    {
        var ciclo = await contextoCiclo.ObtenerAsync();
        if (!ciclo.Disponible) return null;

        var grados = await contexto.Secciones.AsNoTracking()
            .Where(x => x.CicloEscolarId == ciclo.CicloEscolarId!.Value)
            .GroupBy(x => new { x.GradoId, x.Grado.Carrera, x.Grado.Nombre, x.Grado.Nivel, x.Grado.Orden })
            .OrderBy(x => x.Key.Carrera).ThenBy(x => x.Key.Nivel).ThenBy(x => x.Key.Orden)
            .Select(x => new
            {
                x.Key.GradoId,
                x.Key.Carrera,
                x.Key.Nombre,
                Nivel = x.Key.Nivel.ToString(),
                Secciones = x.Count()
            })
            .ToListAsync();

        return new ExploradorAcademicoIndice
        {
            Contexto = CrearContexto(ciclo),
            Carreras = grados.GroupBy(x => string.IsNullOrWhiteSpace(x.Carrera) ? "Sin carrera" : x.Carrera)
                .Select(grupo => new CarreraExploradorAcademico
                {
                    Nombre = grupo.Key,
                    Grados = grupo.Select(x => new GradoExploradorAcademico
                    {
                        Id = x.GradoId,
                        Nombre = x.Nombre,
                        Nivel = x.Nivel,
                        Secciones = x.Secciones
                    }).ToList()
                }).ToList()
        };
    }

    public async Task<ExploradorGradoAcademico?> ObtenerGradoAsync(int gradoId)
    {
        var ciclo = await contextoCiclo.ObtenerAsync();
        if (!ciclo.Disponible || gradoId <= 0) return null;
        var cicloId = ciclo.CicloEscolarId!.Value;
        var esHistorico = ciclo.EsHistorico;

        var grado = await contexto.Secciones.AsNoTracking()
            .Where(x => x.CicloEscolarId == cicloId && x.GradoId == gradoId)
            .Select(x => new { x.GradoId, x.Grado.Carrera, Grado = x.Grado.Nombre })
            .FirstOrDefaultAsync();
        if (grado is null) return null;

        var secciones = await contexto.Secciones.AsNoTracking()
            .Where(x => x.CicloEscolarId == cicloId && x.GradoId == gradoId)
            .OrderBy(x => x.Nombre)
            .Select(x => new SeccionExploradorAcademico
            {
                Id = x.Id,
                Nombre = x.Nombre,
                Estado = x.Estado.ToString(),
                AlumnosVigentes = contexto.Inscripciones.Count(i =>
                    i.SeccionId == x.Id && i.CicloEscolarId == cicloId &&
                    (esHistorico || i.Estado == EstadoInscripcion.Activa)),
                Docentes = contexto.Asignaciones.Where(a => a.SeccionId == x.Id &&
                        (esHistorico || a.Estado == EstadoRegistro.Activo))
                    .Select(a => a.DocenteId).Distinct().Count(),
                Cursos = contexto.Asignaciones.Count(a => a.SeccionId == x.Id &&
                    (esHistorico || a.Estado == EstadoRegistro.Activo))
            }).ToListAsync();

        return new ExploradorGradoAcademico
        {
            Contexto = CrearContexto(ciclo), GradoId = grado.GradoId,
            Carrera = grado.Carrera, Grado = grado.Grado, Secciones = secciones
        };
    }

    public async Task<ExploradorSeccionAcademico?> ObtenerSeccionAsync(int seccionId, ClaimsPrincipal usuario)
    {
        var ciclo = await contextoCiclo.ObtenerAsync();
        if (!ciclo.Disponible || seccionId <= 0) return null;
        var cicloId = ciclo.CicloEscolarId!.Value;
        var esHistorico = ciclo.EsHistorico;
        var seccion = await contexto.Secciones.AsNoTracking()
            .Where(x => x.Id == seccionId && x.CicloEscolarId == cicloId)
            .Select(x => new { x.Id, x.GradoId, Seccion = x.Nombre, Grado = x.Grado.Nombre, x.Grado.Carrera })
            .FirstOrDefaultAsync();
        if (seccion is null) return null;

        var asignaciones = contexto.Asignaciones.AsNoTracking()
            .Where(x => x.SeccionId == seccionId &&
                (esHistorico || x.Estado == EstadoRegistro.Activo));
        var asignacionIds = asignaciones.Select(x => x.Id);
        var puede = new Func<string, bool>(permiso => usuario.HasClaim(TiposClaims.Permiso, permiso));
        var modelo = new ExploradorSeccionAcademico
        {
            Contexto = CrearContexto(ciclo), SeccionId = seccion.Id, GradoId = seccion.GradoId,
            Seccion = seccion.Seccion, Grado = seccion.Grado, Carrera = seccion.Carrera,
            AlumnosVigentes = await contexto.Inscripciones.CountAsync(i => i.SeccionId == seccionId &&
                i.CicloEscolarId == cicloId && (esHistorico || i.Estado == EstadoInscripcion.Activa)),
            Docentes = await asignaciones.Select(a => a.DocenteId).Distinct().CountAsync(),
            Cursos = await asignaciones.CountAsync(),
            Periodos = await contexto.Periodos.CountAsync(p => p.CicloEscolarId == cicloId)
        };

        if (puede(Permisos.Alumnos.Ver))
            modelo.Alumnos = await contexto.Inscripciones.AsNoTracking()
                .Where(x => x.SeccionId == seccionId && x.CicloEscolarId == cicloId &&
                    (esHistorico || x.Estado == EstadoInscripcion.Activa))
                .OrderBy(x => x.Alumno.Usuario.PrimerApellido).ThenBy(x => x.Alumno.Usuario.PrimerNombre)
                .Select(x => new AlumnoExploradorAcademico { Id = x.AlumnoId,
                    Nombre = x.Alumno.Usuario.PrimerNombre + " " + x.Alumno.Usuario.PrimerApellido,
                    Codigo = x.Alumno.CodigoPersonal }).ToListAsync();
        if (puede(Permisos.Asignaciones.Ver))
            modelo.Asignaciones = await asignaciones.OrderBy(x => x.Curso.Nombre).Select(x => new AsignacionExploradorAcademico
            { Id = x.Id, Curso = x.Curso.Nombre, Docente = x.Docente.Usuario.PrimerNombre + " " + x.Docente.Usuario.PrimerApellido }).ToListAsync();
        if (puede(Permisos.Unidades.Ver))
            modelo.Unidades = await contexto.UnidadesAsignaciones.AsNoTracking().Where(x => asignacionIds.Contains(x.AsignacionId))
                .OrderBy(x => x.Orden).Select(x => new ElementoExploradorAcademico { Titulo = x.Unidad.Titulo,
                    Detalle = x.Asignacion.Curso.Nombre + (x.Periodo == null ? "" : " · " + x.Periodo.Nombre) }).ToListAsync();
        if (puede(Permisos.Tareas.Ver))
            modelo.Tareas = await contexto.Tareas.AsNoTracking().Where(x => asignacionIds.Contains(x.AsignacionId))
                .OrderByDescending(x => x.FechaCreacion).Take(20).Select(x => new ElementoExploradorAcademico
                { Titulo = x.Titulo, Detalle = x.Asignacion.Curso.Nombre + " · " + x.Estado.ToString() }).ToListAsync();
        if (puede(Permisos.Cuestionarios.Ver))
            modelo.Cuestionarios = await contexto.Cuestionarios.AsNoTracking().Where(x => asignacionIds.Contains(x.Tarea.AsignacionId))
                .OrderByDescending(x => x.Tarea.FechaCreacion).Take(20).Select(x => new ElementoExploradorAcademico
                { Titulo = x.Tarea.Titulo, Detalle = x.Tarea.Asignacion.Curso.Nombre }).ToListAsync();
        if (puede(Permisos.Materiales.Ver))
            modelo.Materiales = await contexto.Materiales.AsNoTracking().Where(x =>
                    contexto.UnidadesAsignaciones.Any(u => u.UnidadId == x.UnidadId && asignacionIds.Contains(u.AsignacionId)))
                .OrderBy(x => x.Titulo).Select(x => new ElementoExploradorAcademico
                { Titulo = x.Titulo, Detalle = x.Estado.ToString() }).ToListAsync();
        if (puede(Permisos.Planificaciones.Ver))
            modelo.Planificaciones = await contexto.Planificaciones.AsNoTracking().Where(x => x.Asignaciones.Any(a => asignacionIds.Contains(a.AsignacionId)))
                .OrderByDescending(x => x.FechaCreacion).Take(20).Select(x => new ElementoExploradorAcademico
                { Titulo = x.Titulo, Detalle = x.Estado.ToString() }).ToListAsync();
        if (puede(Permisos.Entregas.Ver))
            modelo.Entregas = await contexto.Entregas.AsNoTracking().Where(x => asignacionIds.Contains(x.Tarea.AsignacionId))
                .OrderByDescending(x => x.FechaEntrega).Take(20).Select(x => new ElementoExploradorAcademico
                { Titulo = x.Tarea.Titulo, Detalle = x.Alumno.Usuario.PrimerNombre + " " + x.Alumno.Usuario.PrimerApellido + " · " + x.Estado.ToString() }).ToListAsync();

        AgregarAcceso(modelo.Accesos, puede, Permisos.Inscripciones.Ver, "Inscripciones", "bi bi-person-vcard", "Inscripciones");
        AgregarAcceso(modelo.Accesos, puede, Permisos.Asignaciones.Ver, "Asignaciones", "bi bi-person-workspace", "Asignaciones");
        AgregarAcceso(modelo.Accesos, puede, Permisos.Unidades.Ver, "Unidades", "bi bi-journal-bookmark", "Unidades");
        AgregarAcceso(modelo.Accesos, puede, Permisos.Tareas.Ver, "Tareas", "bi bi-list-check", "Tareas");
        AgregarAcceso(modelo.Accesos, puede, Permisos.Entregas.Ver, "Entregas", "bi bi-inbox", "Entregas");
        AgregarAcceso(modelo.Accesos, puede, Permisos.Calificaciones.Ver, "Calificaciones", "bi bi-journal-check", "Calificaciones");
        return modelo;
    }

    public async Task<ExploradorCursoAcademico?> ObtenerCursoAsync(int asignacionId, ClaimsPrincipal usuario)
    {
        var ciclo = await contextoCiclo.ObtenerAsync();
        if (!ciclo.Disponible || asignacionId <= 0) return null;
        var esHistorico = ciclo.EsHistorico;
        var asignacion = await contexto.Asignaciones.AsNoTracking()
            .Where(x => x.Id == asignacionId && x.Seccion.CicloEscolarId == ciclo.CicloEscolarId!.Value &&
                (esHistorico || x.Estado == EstadoRegistro.Activo))
            .Select(x => new { x.Id, x.SeccionId, x.Seccion.GradoId, Seccion = x.Seccion.Nombre,
                Grado = x.Seccion.Grado.Nombre, x.Seccion.Grado.Carrera, Curso = x.Curso.Nombre,
                Docente = x.Docente.Usuario.PrimerNombre + " " + x.Docente.Usuario.PrimerApellido })
            .FirstOrDefaultAsync();
        if (asignacion is null) return null;

        var puede = new Func<string, bool>(permiso => usuario.HasClaim(TiposClaims.Permiso, permiso));
        var modelo = new ExploradorCursoAcademico
        {
            Contexto = CrearContexto(ciclo), AsignacionId = asignacion.Id, SeccionId = asignacion.SeccionId,
            GradoId = asignacion.GradoId, Seccion = asignacion.Seccion, Grado = asignacion.Grado,
            Carrera = asignacion.Carrera, Curso = asignacion.Curso, Docente = asignacion.Docente,
            Unidades = puede(Permisos.Unidades.Ver) ? await contexto.UnidadesAsignaciones.CountAsync(x => x.AsignacionId == asignacionId) : 0,
            Tareas = puede(Permisos.Tareas.Ver) ? await contexto.Tareas.CountAsync(x => x.AsignacionId == asignacionId) : 0,
            Cuestionarios = puede(Permisos.Cuestionarios.Ver) ? await contexto.Cuestionarios.CountAsync(x => x.Tarea.AsignacionId == asignacionId) : 0,
            Entregas = puede(Permisos.Entregas.Ver) ? await contexto.Entregas.CountAsync(x => x.Tarea.AsignacionId == asignacionId) : 0,
            Materiales = puede(Permisos.Materiales.Ver) ? await contexto.Materiales.CountAsync(x => contexto.UnidadesAsignaciones.Any(u => u.UnidadId == x.UnidadId && u.AsignacionId == asignacionId)) : 0,
            Planificaciones = puede(Permisos.Planificaciones.Ver) ? await contexto.Planificaciones.CountAsync(x => x.Asignaciones.Any(a => a.AsignacionId == asignacionId)) : 0
        };
        if (puede(Permisos.Unidades.Ver)) modelo.Contenido.Add(new() { Titulo = "Unidades", Detalle = $"{modelo.Unidades} registro(s)" });
        if (puede(Permisos.Materiales.Ver)) modelo.Contenido.Add(new() { Titulo = "Materiales", Detalle = $"{modelo.Materiales} registro(s)" });
        if (puede(Permisos.Planificaciones.Ver)) modelo.Contenido.Add(new() { Titulo = "Planificaciones", Detalle = $"{modelo.Planificaciones} registro(s)" });
        if (puede(Permisos.Tareas.Ver)) modelo.Aprendizaje.Add(new() { Titulo = "Tareas", Detalle = $"{modelo.Tareas} registro(s)" });
        if (puede(Permisos.Cuestionarios.Ver)) modelo.Aprendizaje.Add(new() { Titulo = "Cuestionarios", Detalle = $"{modelo.Cuestionarios} registro(s)" });
        if (puede(Permisos.Entregas.Ver)) modelo.Aprendizaje.Add(new() { Titulo = "Entregas", Detalle = $"{modelo.Entregas} registro(s)" });
        AgregarAcceso(modelo.Accesos, puede, Permisos.Unidades.Ver, "Unidades", "bi bi-journal-bookmark", "Unidades");
        AgregarAcceso(modelo.Accesos, puede, Permisos.Materiales.Ver, "Materiales", "bi bi-folder2-open", "Materiales");
        AgregarAcceso(modelo.Accesos, puede, Permisos.Planificaciones.Ver, "Planificaciones", "bi bi-clipboard2-check", "Planificaciones");
        AgregarAcceso(modelo.Accesos, puede, Permisos.Tareas.Ver, "Tareas", "bi bi-list-check", "Tareas");
        AgregarAcceso(modelo.Accesos, puede, Permisos.Cuestionarios.Ver, "Cuestionarios", "bi bi-ui-checks", "Cuestionarios");
        AgregarAcceso(modelo.Accesos, puede, Permisos.Entregas.Ver, "Entregas", "bi bi-inbox", "Entregas");
        AgregarAcceso(modelo.Accesos, puede, Permisos.Calificaciones.Ver, "Calificaciones", "bi bi-journal-check", "Calificaciones");
        return modelo;
    }

    private static ContextoExploradorAcademico CrearContexto(ContextoCicloEscolar ciclo) => new()
    {
        CicloEscolarId = ciclo.CicloEscolarId!.Value, Anio = ciclo.Anio!.Value,
        EsHistorico = ciclo.EsHistorico, Estado = ciclo.EsHistorico ? "Consulta histórica" : "Ciclo institucional activo"
    };

    private static void AgregarAcceso(ICollection<AccesoExploradorAcademico> accesos, Func<string, bool> puede,
        string permiso, string titulo, string icono, string controlador)
    {
        if (puede(permiso)) accesos.Add(new AccesoExploradorAcademico { Titulo = titulo, Icono = icono, Controlador = controlador });
    }
}

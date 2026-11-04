using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.GestionCiclos;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.NavegacionAcademica;

namespace Proyecto_Final.Servicios.NavegacionAcademica;

/// <summary>
/// Compone enlaces de navegación; la autorización de lectura y escritura sigue
/// siendo responsabilidad de los servicios de cada módulo.
/// </summary>
public sealed class NavegacionAcademicaServicio(
    Contexto contexto,
    IContextoCicloEscolar contextoCiclo,
    IAccesoAcademicoServicio acceso) : INavegacionAcademicaServicio
{
    public async Task<MisCursosIndice?> ObtenerMisCursosAsync()
    {
        var ciclo = await contextoCiclo.ObtenerAsync();
        if (!ciclo.Disponible) return null;

        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        var esDocente = perfil.DocenteId.HasValue;
        var consulta = CrearConsultaAutorizada(ciclo.CicloEscolarId!.Value, ciclo.EsHistorico,
            perfil.DocenteId, perfil.AlumnoId);
        var cursos = await Proyectar(consulta, ciclo.Anio!.Value, ciclo.EsHistorico, esDocente).ToListAsync();

        return new MisCursosIndice
        {
            Anio = ciclo.Anio!.Value,
            EsHistorico = ciclo.EsHistorico,
            EsDocente = esDocente,
            Cursos = cursos
        };
    }

    public async Task<EspacioCurso?> ObtenerEspacioAsync(int asignacionId)
    {
        if (asignacionId <= 0) return null;
        var ciclo = await contextoCiclo.ObtenerAsync();
        if (!ciclo.Disponible) return null;

        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        var esDocente = perfil.DocenteId.HasValue;
        var consulta = CrearConsultaAutorizada(ciclo.CicloEscolarId!.Value, ciclo.EsHistorico,
            perfil.DocenteId, perfil.AlumnoId).Where(x => x.Id == asignacionId);
        var curso = await Proyectar(consulta, ciclo.Anio!.Value, ciclo.EsHistorico, esDocente).SingleOrDefaultAsync();
        if (curso is null) return null;

        var contenido = new List<EnlaceCursoContextual>();
        var aprendizaje = new List<EnlaceCursoContextual>();
        Agregar(contenido, Permisos.Unidades.Ver, "Unidades", "Unidades", "bi bi-journal-bookmark");
        Agregar(contenido, Permisos.Materiales.Ver, "Materiales", "Materiales", "bi bi-folder2-open");
        if (esDocente || acceso.EsOperadorInstitucional)
        {
            Agregar(contenido, Permisos.Planificaciones.Ver, "Planificación", "Planificaciones", "bi bi-clipboard2-check");
        }
        Agregar(aprendizaje, Permisos.Tareas.Ver, "Tareas", "Tareas", "bi bi-list-check");
        Agregar(aprendizaje, Permisos.Entregas.Ver, "Entregas", "Entregas", "bi bi-inbox");
        Agregar(aprendizaje, Permisos.Cuestionarios.Ver, "Cuestionarios", "Cuestionarios", "bi bi-ui-checks");
        Agregar(aprendizaje, Permisos.Calificaciones.Ver, "Calificaciones", "Calificaciones", "bi bi-journal-check");

        return new EspacioCurso { Contexto = curso, Contenido = contenido, Aprendizaje = aprendizaje };
    }

    private IQueryable<Asignacion> CrearConsultaAutorizada(int cicloId, bool historico, int? docenteId, int? alumnoId)
    {
        var q = contexto.Asignaciones.AsNoTracking().Where(x =>
            x.Seccion.CicloEscolarId == cicloId && (historico || x.Estado == EstadoRegistro.Activo));

        if (acceso.EsOperadorInstitucional) return q;
        if (docenteId.HasValue)
        {
            var ahora = DateTime.UtcNow;
            return q.Where(x => x.DocenteId == docenteId.Value ||
                contexto.DelegacionesAcademicas.Any(d =>
                    d.AsignacionId == x.Id &&
                    d.DocenteId == docenteId.Value &&
                    d.Activa && d.FechaInicioUtc <= ahora &&
                    (!d.FechaFinUtc.HasValue || d.FechaFinUtc >= ahora) &&
                    (d.Operaciones & OperacionDelegacionAcademica.GestionarEvaluaciones) ==
                        OperacionDelegacionAcademica.GestionarEvaluaciones));
        }
        if (alumnoId.HasValue)
            return q.Where(x => contexto.Inscripciones.Any(i =>
                i.AlumnoId == alumnoId.Value && i.SeccionId == x.SeccionId &&
                i.CicloEscolarId == cicloId && (historico || i.Estado == EstadoInscripcion.Activa)));
        return q.Where(_ => false);
    }

    private static IQueryable<CursoContextual> Proyectar(IQueryable<Asignacion> q, int anio, bool historico, bool esDocente) =>
        q.OrderBy(x => x.Curso.Nombre).ThenBy(x => x.Seccion.Grado.Orden).ThenBy(x => x.Seccion.Nombre)
            .Select(x => new CursoContextual
            {
                AsignacionId = x.Id,
                CicloEscolarId = x.Seccion.CicloEscolarId,
                Anio = anio,
                EsHistorico = historico,
                EsDocente = esDocente,
                Curso = x.Curso.Nombre,
                Grado = x.Seccion.Grado.Nombre,
                Seccion = x.Seccion.Nombre,
                Docente = x.Docente.Usuario.PrimerNombre + " " + x.Docente.Usuario.PrimerApellido
            });

    private void Agregar(ICollection<EnlaceCursoContextual> destino, string permiso, string titulo,
        string controlador, string icono, string accion = "Index")
    {
        if (acceso.TienePermiso(permiso)) destino.Add(new EnlaceCursoContextual
        {
            Titulo = titulo, Controlador = controlador, Icono = icono, Accion = accion
        });
    }
}

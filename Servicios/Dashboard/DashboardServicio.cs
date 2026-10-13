using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Prototipo;

namespace Proyecto_Final.Servicios.Dashboard;

public sealed class DashboardServicio(
    Contexto contexto,
    IAccesoAcademicoServicio acceso) : IDashboardServicio
{
    public async Task<ResultadoDashboard> ObtenerAsync(
        ClaimsPrincipal usuarioActual,
        string? contextoSolicitado = null)
    {
        var docenteId = await acceso.ObtenerDocenteIdAsync();
        var alumnoId = await acceso.ObtenerAlumnoIdAsync();
        var puedeCoordinar = Tiene(
            usuarioActual,
            Permisos.Dashboard.CoordinacionVer);

        var contextos = new List<ContextoDashboard>();
        if (alumnoId.HasValue)
        {
            contextos.Add(new ContextoDashboard
            {
                Codigo = "alumno",
                Titulo = "Mi espacio académico",
                Descripcion = "Cursos, tareas y entregas de su inscripción vigente.",
                Icono = "bi bi-mortarboard"
            });
        }

        if (docenteId.HasValue)
        {
            contextos.Add(new ContextoDashboard
            {
                Codigo = "docente",
                Titulo = "Dashboard docente",
                Descripcion = "Cursos, planificaciones, tareas y entregas a su cargo.",
                Icono = "bi bi-person-workspace"
            });
        }

        if (puedeCoordinar)
        {
            contextos.Add(new ContextoDashboard
            {
                Codigo = "coordinacion",
                Titulo = "Dashboard de coordinación",
                Descripcion = "Seguimiento académico institucional según sus permisos.",
                Icono = "bi bi-diagram-3"
            });
        }

        var codigo = contextoSolicitado?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(codigo))
        {
            codigo = contextos.Count == 1 ? contextos[0].Codigo : "inicio";
        }

        if (codigo != "inicio" && contextos.All(x => x.Codigo != codigo))
        {
            return new ResultadoDashboard { Permitido = false };
        }

        ResultadoDashboard resultado = codigo switch
        {
            "alumno" when alumnoId.HasValue =>
                await ObtenerAlumnoAsync(alumnoId.Value, usuarioActual),
            "docente" when docenteId.HasValue =>
                await ObtenerDocenteAsync(docenteId.Value, usuarioActual),
            "coordinacion" when puedeCoordinar =>
                await ObtenerCoordinacionAsync(usuarioActual),
            _ => ObtenerInicio(usuarioActual)
        };

        resultado.Modelo.Contextos = contextos;
        return resultado;
    }

    private ResultadoDashboard ObtenerInicio(ClaimsPrincipal usuario)
    {
        var modelo = new DashboardPrototipo
        {
            Tipo = "inicio",
            Accesos = CrearAccesos(usuario)
        };

        return new ResultadoDashboard
        {
            Titulo = "Inicio",
            Descripcion = "Bienvenido a SabIA. Seleccione un espacio o ingrese a uno de los módulos disponibles.",
            Modelo = modelo
        };
    }

    private async Task<ResultadoDashboard> ObtenerCoordinacionAsync(
        ClaimsPrincipal usuario)
    {
        var modelo = new DashboardPrototipo
        {
            Tipo = "coordinacion",
            Accesos = CrearAccesos(usuario)
        };

        if (Tiene(usuario, Permisos.Docentes.Ver))
        {
            modelo.Indicadores.Add(Indicador(
                "Docentes activos",
                await contexto.Docentes.CountAsync(x => x.Usuario.Activo),
                "bi bi-person-badge",
                "ui-stat-card-primary"));
        }

        if (Tiene(usuario, Permisos.Alumnos.Ver))
        {
            modelo.Indicadores.Add(Indicador(
                "Alumnos activos",
                await contexto.Alumnos.CountAsync(x => x.Usuario.Activo),
                "bi bi-people",
                "ui-stat-card-success"));
        }

        if (Tiene(usuario, Permisos.Asignaciones.Ver))
        {
            var asignaciones = await contexto.Asignaciones.CountAsync(x =>
                x.Estado == EstadoRegistro.Activo &&
                x.Seccion.CicloEscolar.Activo);
            modelo.Indicadores.Add(Indicador(
                "Asignaciones del ciclo activo",
                asignaciones,
                "bi bi-journal-bookmark",
                "ui-stat-card-info"));

            modelo.Cursos = await ConsultaCursosActuales()
                .OrderBy(x => x.Curso.Nombre)
                .Take(12)
                .Select(x => new CursoResumenPrototipo
                {
                    Curso = x.Curso.Nombre,
                    GradoSeccion = x.Seccion.Grado.Nombre + " " + x.Seccion.Nombre,
                    Docente = x.Docente.Usuario.PrimerNombre + " " +
                        x.Docente.Usuario.PrimerApellido,
                    Progreso = contexto.UnidadesAsignaciones.Any(u =>
                        u.AsignacionId == x.Id)
                        ? (int)Math.Round(100.0 *
                            contexto.UnidadesAsignaciones.Count(u =>
                                u.AsignacionId == x.Id &&
                                u.Estado == EstadoPublicacionUnidad.Publicada) /
                            contexto.UnidadesAsignaciones.Count(u =>
                                u.AsignacionId == x.Id))
                        : 0,
                    EntregasPendientes = contexto.Entregas.Count(e =>
                        e.Tarea.AsignacionId == x.Id &&
                        (e.Estado == EstadoEntrega.Enviada ||
                         e.Estado == EstadoEntrega.Tardia) &&
                        !contexto.Entregas.Any(otra =>
                            otra.TareaId == e.TareaId &&
                            otra.AlumnoId == e.AlumnoId &&
                            otra.NumeroEnvio > e.NumeroEnvio))
                })
                .ToListAsync();
        }

        if (Tiene(usuario, Permisos.Planificaciones.Ver))
        {
            var revisiones = await contexto.Planificaciones.CountAsync(x =>
                x.Estado == EstadoPlanificacion.EnRevision &&
                x.Asignaciones.Any(a => a.Asignacion.Seccion.CicloEscolar.Activo));
            modelo.Indicadores.Add(Indicador(
                "Planificaciones por revisar",
                revisiones,
                "bi bi-clipboard-check",
                "ui-stat-card-warning"));

            if (revisiones > 0)
            {
                modelo.Alertas.Add(new AlertaPrototipo
                {
                    Tipo = "Planificaciones",
                    Mensaje = $"Hay {revisiones} planificación(es) pendientes de revisión.",
                    Clase = "warning"
                });
            }
        }

        if (Tiene(usuario, Permisos.Calificaciones.Ver))
        {
            var configurados = await contexto.ConfiguracionesEvaluacion
                .AsNoTracking()
                .CountAsync(x => x.Asignacion.Seccion.CicloEscolar.Activo);
            var cerrados = await contexto.CierresCalificaciones
                .AsNoTracking()
                .CountAsync(x =>
                    x.Estado == EstadoCierreCalificaciones.Cerrado &&
                    x.ConfiguracionEvaluacion.Asignacion.Seccion.CicloEscolar.Activo);
            modelo.Indicadores.Add(Indicador(
                "Avance de cierres",
                configurados == 0 ? "0/0" : $"{cerrados}/{configurados}",
                "bi bi-journal-check",
                "ui-stat-card-info"));
        }

        if (Tiene(usuario, Permisos.Tareas.Ver))
        {
            modelo.ActividadReciente = await ActividadRecienteAsync(null);
        }

        AgregarEstadoVacio(modelo);
        return new ResultadoDashboard
        {
            Titulo = "Dashboard de coordinación",
            Descripcion = "Seguimiento general del ciclo escolar activo según sus permisos.",
            Modelo = modelo
        };
    }

    private async Task<ResultadoDashboard> ObtenerDocenteAsync(
        int docenteId,
        ClaimsPrincipal usuario)
    {
        var modelo = new DashboardPrototipo
        {
            Tipo = "docente",
            Accesos = CrearAccesos(usuario)
        };
        var asignacionesIds = ConsultaCursosActuales()
            .Where(x => x.DocenteId == docenteId)
            .Select(x => x.Id);
        var seccionesIds = ConsultaCursosActuales()
            .Where(x => x.DocenteId == docenteId)
            .Select(x => x.SeccionId)
            .Distinct();

        if (TieneAlguno(usuario,
            Permisos.Tareas.Ver,
            Permisos.Unidades.Ver,
            Permisos.Planificaciones.Ver,
            Permisos.Entregas.Ver))
        {
            var cursos = await asignacionesIds.CountAsync();
            modelo.Indicadores.Add(Indicador(
                "Cursos asignados", cursos, "bi bi-journal-bookmark",
                "ui-stat-card-primary"));

            modelo.Cursos = await ConsultaCursosActuales()
                .Where(x => x.DocenteId == docenteId)
                .OrderBy(x => x.Curso.Nombre)
                .Select(x => new CursoResumenPrototipo
                {
                    Curso = x.Curso.Nombre,
                    GradoSeccion = x.Seccion.Grado.Nombre + " " + x.Seccion.Nombre,
                    Docente = x.Docente.Usuario.PrimerNombre + " " +
                        x.Docente.Usuario.PrimerApellido,
                    Progreso = contexto.UnidadesAsignaciones.Any(u =>
                        u.AsignacionId == x.Id)
                        ? (int)Math.Round(100.0 *
                            contexto.UnidadesAsignaciones.Count(u =>
                                u.AsignacionId == x.Id &&
                                u.Estado == EstadoPublicacionUnidad.Publicada) /
                            contexto.UnidadesAsignaciones.Count(u =>
                                u.AsignacionId == x.Id))
                        : 0,
                    EntregasPendientes = contexto.Entregas.Count(e =>
                        e.Tarea.AsignacionId == x.Id &&
                        (e.Estado == EstadoEntrega.Enviada ||
                         e.Estado == EstadoEntrega.Tardia) &&
                        !contexto.Entregas.Any(otra =>
                            otra.TareaId == e.TareaId &&
                            otra.AlumnoId == e.AlumnoId &&
                            otra.NumeroEnvio > e.NumeroEnvio))
                })
                .ToListAsync();
        }

        if (Tiene(usuario, Permisos.Alumnos.Ver))
        {
            var alumnos = await contexto.Inscripciones
                .Where(x =>
                    x.Estado == EstadoInscripcion.Activa &&
                    x.Seccion.CicloEscolar.Activo &&
                    seccionesIds.Contains(x.SeccionId))
                .Select(x => x.AlumnoId)
                .Distinct()
                .CountAsync();
            modelo.Indicadores.Add(Indicador(
                "Alumnos", alumnos, "bi bi-people", "ui-stat-card-success"));
        }

        if (Tiene(usuario, Permisos.Tareas.Ver))
        {
            var tareasPorEstado = await contexto.Tareas
                .Where(x => asignacionesIds.Contains(x.AsignacionId))
                .GroupBy(x => x.Estado)
                .Select(grupo => new { Estado = grupo.Key, Total = grupo.Count() })
                .ToListAsync();
            var publicadas = tareasPorEstado
                .Where(x => x.Estado == EstadoTarea.Publicada)
                .Sum(x => x.Total);
            var borradores = tareasPorEstado
                .Where(x => x.Estado == EstadoTarea.Borrador)
                .Sum(x => x.Total);
            var cerradas = tareasPorEstado
                .Where(x => x.Estado == EstadoTarea.Cerrada)
                .Sum(x => x.Total);
            modelo.Indicadores.Add(Indicador(
                "Tareas publicadas", publicadas, "bi bi-list-check",
                "ui-stat-card-info"));
            modelo.Indicadores.Add(Indicador(
                "Tareas pendientes de publicar", borradores, "bi bi-pencil-square",
                "ui-stat-card-warning"));
            modelo.Indicadores.Add(Indicador(
                "Tareas cerradas", cerradas, "bi bi-lock",
                "ui-stat-card-primary"));
            modelo.ActividadReciente = await ActividadRecienteAsync(docenteId);
        }

        if (Tiene(usuario, Permisos.Entregas.Ver))
        {
            var entregasPendientes = contexto.Entregas
                .AsNoTracking()
                .Where(x =>
                    asignacionesIds.Contains(x.Tarea.AsignacionId) &&
                    (x.Estado == EstadoEntrega.Enviada ||
                     x.Estado == EstadoEntrega.Tardia) &&
                    !contexto.Entregas.Any(otra =>
                        otra.TareaId == x.TareaId &&
                        otra.AlumnoId == x.AlumnoId &&
                        otra.NumeroEnvio > x.NumeroEnvio));

            var totalPorRevisar = await entregasPendientes.CountAsync();
            modelo.Entregas = await entregasPendientes
                .OrderBy(x => x.FechaEntrega)
                .Take(10)
                .Select(x => new EntregaPendientePrototipo
                {
                    Id = x.Id,
                    Alumno = x.Alumno.Usuario.PrimerNombre + " " +
                        x.Alumno.Usuario.PrimerApellido,
                    Tarea = x.Tarea.Titulo,
                    Curso = x.Tarea.Asignacion.Curso.Nombre,
                    Fecha = x.FechaEntrega.HasValue
                        ? x.FechaEntrega.Value.ToString("dd/MM/yyyy HH:mm")
                        : "Sin fecha"
                })
                .ToListAsync();

            modelo.Indicadores.Add(Indicador(
                "Entregas por revisar", totalPorRevisar,
                "bi bi-inbox", "ui-stat-card-warning"));

            modelo.Reaperturas = await contexto.Entregas
                .AsNoTracking()
                .Where(x =>
                    asignacionesIds.Contains(x.Tarea.AsignacionId) &&
                    x.Estado == EstadoEntrega.Pendiente &&
                    x.FechaLimiteIndividual.HasValue &&
                    x.FechaLimiteIndividual >= DateTime.UtcNow)
                .OrderBy(x => x.FechaLimiteIndividual)
                .Take(10)
                .Select(x => new ReaperturaResumenDashboard
                {
                    Alumno = x.Alumno.Usuario.PrimerNombre + " " +
                        x.Alumno.Usuario.PrimerApellido,
                    Tarea = x.Tarea.Titulo,
                    FechaLimite = x.FechaLimiteIndividual!.Value
                })
                .ToListAsync();
        }

        if (Tiene(usuario, Permisos.Planificaciones.Ver))
        {
            modelo.Planificaciones = await contexto.Planificaciones
                .AsNoTracking()
                .Where(x =>
                    x.DocenteId == docenteId &&
                    x.Asignaciones.Any(a =>
                        a.Asignacion.Seccion.CicloEscolar.Activo))
                .OrderByDescending(x => x.FechaCreacion)
                .Take(8)
                .Select(x => new PlanificacionResumenDashboard
                {
                    Titulo = x.Titulo,
                    Estado = x.Estado.ToString(),
                    Periodo = x.Asignaciones
                        .Where(a => a.Periodo != null)
                        .Select(a => a.Periodo!.Nombre)
                        .FirstOrDefault() ?? "Sin período"
                })
                .ToListAsync();

            var devueltas = modelo.Planificaciones.Count(x =>
                x.Estado == EstadoPlanificacion.Devuelta.ToString());
            if (devueltas > 0)
            {
                modelo.Alertas.Add(new AlertaPrototipo
                {
                    Tipo = "Planificaciones",
                    Mensaje = $"Tiene {devueltas} planificación(es) devueltas para corrección.",
                    Clase = "warning"
                });
            }
        }

        if (Tiene(usuario, Permisos.Calificaciones.Ver))
        {
            var pendientes = await contexto.ActividadesEvaluables
                .AsNoTracking()
                .Where(a =>
                    a.Origen == OrigenActividadEvaluable.Manual &&
                    a.ConfiguracionEvaluacion.Asignacion.DocenteId == docenteId &&
                    a.ConfiguracionEvaluacion.Asignacion.Estado == EstadoRegistro.Activo &&
                    a.ConfiguracionEvaluacion.Asignacion.Seccion.CicloEscolar.Activo &&
                    !contexto.CierresCalificaciones.Any(c =>
                        c.ConfiguracionEvaluacionId == a.ConfiguracionEvaluacionId &&
                        c.Estado == EstadoCierreCalificaciones.Cerrado))
                .SelectMany(a => contexto.Inscripciones
                    .Where(i =>
                        i.SeccionId == a.ConfiguracionEvaluacion.SeccionId &&
                        i.CicloEscolarId == a.ConfiguracionEvaluacion.CicloEscolarId &&
                        i.Estado == EstadoInscripcion.Activa &&
                        !contexto.CalificacionesManuales.Any(c =>
                            c.ActividadEvaluableId == a.Id &&
                            c.InscripcionId == i.Id &&
                            c.Nota.HasValue)))
                .CountAsync();
            modelo.Indicadores.Add(Indicador(
                "Calificaciones pendientes", pendientes,
                "bi bi-journal-x", "ui-stat-card-warning"));

            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
            var limite = hoy.AddDays(14);
            var proximos = await contexto.ConfiguracionesEvaluacion
                .AsNoTracking()
                .CountAsync(x =>
                    x.Asignacion.DocenteId == docenteId &&
                    x.Asignacion.Seccion.CicloEscolar.Activo &&
                    x.Periodo.FechaFin >= hoy &&
                    x.Periodo.FechaFin <= limite &&
                    !contexto.CierresCalificaciones.Any(c =>
                        c.ConfiguracionEvaluacionId == x.Id &&
                        c.Estado == EstadoCierreCalificaciones.Cerrado));
            modelo.Indicadores.Add(Indicador(
                "Períodos próximos al cierre", proximos,
                "bi bi-calendar-event", "ui-stat-card-danger"));
        }

        if (Tiene(usuario, Permisos.Notificaciones.Ver) &&
            modelo.Entregas.Count > 0)
        {
            modelo.Alertas.Add(new AlertaPrototipo
            {
                Tipo = "Entregas",
                Mensaje = "Tiene entregas recientes pendientes de calificación.",
                Clase = "info"
            });
        }

        AgregarEstadoVacio(modelo);
        return new ResultadoDashboard
        {
            Titulo = "Mi dashboard docente",
            Descripcion = "Resumen de sus cursos, planificaciones, tareas y entregas pendientes.",
            Modelo = modelo
        };
    }

    private async Task<ResultadoDashboard> ObtenerAlumnoAsync(
        int alumnoId,
        ClaimsPrincipal usuario)
    {
        var modelo = new DashboardPrototipo
        {
            Tipo = "alumno",
            Accesos = CrearAccesos(usuario)
        };

        var inscripcion = await contexto.Inscripciones
            .AsNoTracking()
            .Where(x =>
                x.AlumnoId == alumnoId &&
                x.Estado == EstadoInscripcion.Activa &&
                x.Seccion.CicloEscolar.Activo)
            .OrderByDescending(x => x.Fecha)
            .Select(x => new { x.Id, x.SeccionId })
            .FirstOrDefaultAsync();

        if (inscripcion == null)
        {
            modelo.Alertas.Add(new AlertaPrototipo
            {
                Tipo = "Inscripción",
                Mensaje = "No tiene una inscripción académica activa.",
                Clase = "warning"
            });
            return new ResultadoDashboard
            {
                Titulo = "Mi espacio académico",
                Descripcion = "Consulte aquí su actividad académica.",
                Modelo = modelo
            };
        }

        var asignacionesIds = contexto.Asignaciones
            .Where(x =>
                x.SeccionId == inscripcion.SeccionId &&
                x.Estado == EstadoRegistro.Activo)
            .Select(x => x.Id);

        if (TieneAlguno(usuario, Permisos.Unidades.Ver, Permisos.Tareas.Ver))
        {
            modelo.Indicadores.Add(Indicador(
                "Cursos", await asignacionesIds.CountAsync(),
                "bi bi-journal-bookmark", "ui-stat-card-primary"));

            modelo.Cursos = await contexto.Asignaciones
                .AsNoTracking()
                .Where(x => asignacionesIds.Contains(x.Id))
                .OrderBy(x => x.Curso.Nombre)
                .Select(x => new CursoResumenPrototipo
                {
                    Curso = x.Curso.Nombre,
                    GradoSeccion = x.Seccion.Grado.Nombre + " " + x.Seccion.Nombre,
                    Docente = x.Docente.Usuario.PrimerNombre + " " +
                        x.Docente.Usuario.PrimerApellido,
                    Progreso = contexto.Entregas.Any(e =>
                        e.AlumnoId == alumnoId &&
                        e.Tarea.AsignacionId == x.Id &&
                        !contexto.Entregas.Any(otra =>
                            otra.TareaId == e.TareaId &&
                            otra.AlumnoId == e.AlumnoId &&
                            otra.NumeroEnvio > e.NumeroEnvio))
                        ? (int)Math.Round(100.0 * contexto.Entregas.Count(e =>
                            e.AlumnoId == alumnoId &&
                            e.Tarea.AsignacionId == x.Id &&
                            e.Estado != EstadoEntrega.Pendiente &&
                            !contexto.Entregas.Any(otra =>
                                otra.TareaId == e.TareaId &&
                                otra.AlumnoId == e.AlumnoId &&
                                otra.NumeroEnvio > e.NumeroEnvio)) /
                            contexto.Entregas.Count(e =>
                                e.AlumnoId == alumnoId &&
                                e.Tarea.AsignacionId == x.Id &&
                                !contexto.Entregas.Any(otra =>
                                    otra.TareaId == e.TareaId &&
                                    otra.AlumnoId == e.AlumnoId &&
                                    otra.NumeroEnvio > e.NumeroEnvio)))
                        : 0,
                    EntregasPendientes = contexto.Entregas.Count(e =>
                        e.AlumnoId == alumnoId &&
                        e.Tarea.AsignacionId == x.Id &&
                        e.Estado == EstadoEntrega.Pendiente &&
                        !contexto.Entregas.Any(otra =>
                            otra.TareaId == e.TareaId &&
                            otra.AlumnoId == e.AlumnoId &&
                            otra.NumeroEnvio > e.NumeroEnvio))
                })
                .ToListAsync();
        }

        if (Tiene(usuario, Permisos.Unidades.Ver))
        {
            var ahora = DateTime.UtcNow;
            var unidades = await contexto.UnidadesAsignaciones.CountAsync(x =>
                asignacionesIds.Contains(x.AsignacionId) &&
                x.Estado == EstadoPublicacionUnidad.Publicada &&
                (!x.FechaDisponibilidad.HasValue || x.FechaDisponibilidad <= ahora) &&
                (!x.FechaCierreAcceso.HasValue || x.FechaCierreAcceso >= ahora));
            modelo.Indicadores.Add(Indicador(
                "Unidades disponibles", unidades, "bi bi-collection",
                "ui-stat-card-success"));
        }

        if (Tiene(usuario, Permisos.Entregas.Ver))
        {
            var actuales = contexto.Entregas.Where(x =>
                x.AlumnoId == alumnoId &&
                asignacionesIds.Contains(x.Tarea.AsignacionId) &&
                !contexto.Entregas.Any(otra =>
                    otra.TareaId == x.TareaId &&
                    otra.AlumnoId == x.AlumnoId &&
                    otra.NumeroEnvio > x.NumeroEnvio));
            var pendientes = await actuales.CountAsync(x =>
                x.Estado == EstadoEntrega.Pendiente);
            var realizadas = await actuales.CountAsync(x =>
                x.Estado != EstadoEntrega.Pendiente);

            modelo.Indicadores.Add(Indicador(
                "Tareas pendientes", pendientes, "bi bi-list-check",
                "ui-stat-card-warning"));
            modelo.Indicadores.Add(Indicador(
                "Entregas realizadas", realizadas, "bi bi-check2-circle",
                "ui-stat-card-info"));

            var historicas = await contexto.Entregas.CountAsync(x =>
                x.AlumnoId == alumnoId &&
                !asignacionesIds.Contains(x.Tarea.AsignacionId) &&
                !contexto.Entregas.Any(otra =>
                    otra.TareaId == x.TareaId &&
                    otra.AlumnoId == x.AlumnoId &&
                    otra.NumeroEnvio > x.NumeroEnvio));
            modelo.Indicadores.Add(Indicador(
                "Actividades históricas", historicas, "bi bi-archive",
                "ui-stat-card-primary"));
            modelo.Alertas.Add(new AlertaPrototipo
            {
                Tipo = pendientes > 0 ? "Tareas" : "Seguimiento",
                Mensaje = pendientes > 0
                    ? $"Tiene {pendientes} tarea(s) pendientes de entrega."
                    : "No tiene tareas pendientes en su inscripción vigente.",
                Clase = pendientes > 0 ? "warning" : "success"
            });
        }

        if (Tiene(usuario, Permisos.Calificaciones.Ver))
        {
            var resultados = await contexto.ResultadosCalificacionesPeriodos
                .AsNoTracking()
                .Where(x => x.AlumnoId == alumnoId)
                .OrderByDescending(x => x.FechaCierre)
                .Select(x => new { x.NotaBimestral })
                .ToListAsync();
            modelo.Indicadores.Add(Indicador(
                "Resultados bimestrales", resultados.Count,
                "bi bi-journal-check", "ui-stat-card-info"));
            if (resultados.Count > 0)
            {
                modelo.Indicadores.Add(Indicador(
                    "Última nota cerrada",
                    resultados[0].NotaBimestral.ToString("0.####"),
                    "bi bi-award", "ui-stat-card-success"));
            }
        }

        AgregarEstadoVacio(modelo);
        return new ResultadoDashboard
        {
            Titulo = "Mi espacio académico",
            Descripcion = "Cursos y actividades de su inscripción vigente.",
            Modelo = modelo
        };
    }

    private IQueryable<Asignacion> ConsultaCursosActuales() =>
        contexto.Asignaciones
            .AsNoTracking()
            .Where(x =>
                x.Estado == EstadoRegistro.Activo &&
                x.Seccion.CicloEscolar.Activo);

    private async Task<List<ActividadPrototipo>> ActividadRecienteAsync(
        int? docenteId)
    {
        var tareas = contexto.Tareas
            .AsNoTracking()
            .Where(x =>
                x.FechaPublicacion.HasValue &&
                x.Asignacion.Seccion.CicloEscolar.Activo);

        if (docenteId.HasValue)
        {
            tareas = tareas.Where(x =>
                x.Asignacion.DocenteId == docenteId.Value);
        }

        return await tareas
            .OrderByDescending(x => x.FechaPublicacion)
            .Take(6)
            .Select(x => new ActividadPrototipo
            {
                Icono = "bi bi-list-check",
                Titulo = "Tarea publicada",
                Detalle = x.Titulo + " · " + x.Asignacion.Curso.Nombre,
                Hace = x.FechaPublicacion!.Value.ToString("dd/MM/yyyy")
            })
            .ToListAsync();
    }

    private static List<AccesoDashboard> CrearAccesos(ClaimsPrincipal usuario)
    {
        var accesos = new List<AccesoDashboard>();
        AgregarAcceso(accesos, usuario, Permisos.Alumnos.Ver,
            "Alumnos", "Consulte alumnos autorizados.", "Alumnos", "bi bi-people");
        AgregarAcceso(accesos, usuario, Permisos.Docentes.Ver,
            "Docentes", "Consulte el personal docente.", "Docentes", "bi bi-person-badge");
        AgregarAcceso(accesos, usuario, Permisos.Asignaciones.Ver,
            "Asignaciones", "Revise cursos y secciones asignados.", "Asignaciones", "bi bi-journal-bookmark");
        AgregarAcceso(accesos, usuario, Permisos.Planificaciones.Ver,
            "Planificaciones", "Consulte la planificación académica.", "Planificaciones", "bi bi-clipboard-check");
        AgregarAcceso(accesos, usuario, Permisos.Tareas.Ver,
            "Tareas", "Consulte tareas y fechas de entrega.", "Tareas", "bi bi-list-check");
        AgregarAcceso(accesos, usuario, Permisos.Entregas.Ver,
            "Entregas", "Consulte entregas autorizadas.", "Entregas", "bi bi-inbox");
        AgregarAcceso(accesos, usuario, Permisos.Calificaciones.Ver,
            "Calificaciones", "Consulte el libro y sus resultados autorizados.", "Calificaciones", "bi bi-journal-check");
        AgregarAcceso(accesos, usuario, Permisos.Notificaciones.Ver,
            "Notificaciones", "Consulte sus avisos académicos.", "Notificaciones", "bi bi-bell");
        return accesos;
    }

    private static void AgregarAcceso(
        ICollection<AccesoDashboard> accesos,
        ClaimsPrincipal usuario,
        string permiso,
        string titulo,
        string descripcion,
        string controlador,
        string icono)
    {
        if (Tiene(usuario, permiso))
        {
            accesos.Add(new AccesoDashboard
            {
                Titulo = titulo,
                Descripcion = descripcion,
                Controlador = controlador,
                Icono = icono
            });
        }
    }

    private static bool Tiene(ClaimsPrincipal usuario, string permiso) =>
        usuario.HasClaim(TiposClaims.Permiso, permiso);

    private static bool TieneAlguno(
        ClaimsPrincipal usuario,
        params string[] permisos) => permisos.Any(x => Tiene(usuario, x));

    private static IndicadorPrototipo Indicador(
        string titulo,
        int valor,
        string icono,
        string clase) => new()
    {
        Titulo = titulo,
        Valor = valor.ToString(),
        Icono = icono,
        Clase = clase
    };

    private static IndicadorPrototipo Indicador(
        string titulo,
        string valor,
        string icono,
        string clase) => new()
    {
        Titulo = titulo,
        Valor = valor,
        Icono = icono,
        Clase = clase
    };

    private static void AgregarEstadoVacio(DashboardPrototipo modelo)
    {
        if (modelo.Alertas.Count == 0 &&
            modelo.Indicadores.Count == 0 &&
            modelo.Cursos.Count == 0 &&
            modelo.Planificaciones.Count == 0 &&
            modelo.Entregas.Count == 0)
        {
            modelo.Alertas.Add(new AlertaPrototipo
            {
                Tipo = "Información",
                Mensaje = "No hay información disponible con sus permisos actuales.",
                Clase = "info"
            });
        }
    }
}

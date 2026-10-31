using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.Servicios.GestionCiclos;
using Proyecto_Final.ViewModels.Notificaciones;

namespace Proyecto_Final.Servicios.GestionNotificaciones;

public sealed class NotificacionServicio(
    Contexto contexto,
    IAccesoAcademicoServicio acceso,
    IContextoCicloEscolar? contextoCiclo = null) : INotificacionServicio
{
    public async Task<List<NotificacionLista>> ObtenerAsync() =>
        (await ObtenerResumenAsync()).Notificaciones;

    public async Task<ResumenNotificaciones> ObtenerResumenAsync()
    {
        var resultado = new ResumenNotificaciones
        {
            PuedeVer = acceso.TienePermiso(Permisos.Notificaciones.Ver)
        };
        if (!resultado.PuedeVer)
            return resultado;
        var cicloId = contextoCiclo is null ? null : (await contextoCiclo.ObtenerAsync()).CicloEscolarId;
        if (contextoCiclo is not null && !cicloId.HasValue)
            return resultado;
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        var ahora = DateTime.UtcNow;
        if (perfil.AlumnoId.HasValue) await AgregarAlumnoAsync(resultado, perfil.AlumnoId.Value, ahora, cicloId);
        if (perfil.DocenteId.HasValue) await AgregarDocenteAsync(resultado, perfil.DocenteId.Value, ahora, cicloId);
        if (acceso.EsOperadorInstitucional && acceso.TienePermiso(Permisos.Planificaciones.Ver) &&
            acceso.TienePermiso(Permisos.Planificaciones.Revisar))
        {
            var total = await contexto.Planificaciones.CountAsync(x => x.Estado == EstadoPlanificacion.EnRevision &&
                x.Asignaciones.Any(a => cicloId.HasValue
                    ? a.Asignacion.Seccion.CicloEscolarId == cicloId.Value
                    : a.Asignacion.Seccion.CicloEscolar.Activo));
            if (total > 0) resultado.Notificaciones.Add(Alerta("Planificaciones en revisión",
                $"Hay {total} planificación(es) pendientes de revisión.", "bi bi-clipboard2-check", "warning", "Planificaciones", ahora));
        }
        resultado.NovedadesNoVisualizadas = resultado.Notificaciones.Count(x => x.EsNovedad);
        resultado.Notificaciones = resultado.Notificaciones.OrderByDescending(x => x.EsNovedad)
            .ThenByDescending(x => x.Fecha).ToList();
        return resultado;
    }

    public async Task RegistrarVisualizacionActividadAsync(int tareaId, CancellationToken ct = default)
    {
        var alumnoId = await acceso.ObtenerAlumnoIdAsync();
        if (!alumnoId.HasValue) return;
        var ahora = DateTime.UtcNow;
        var cicloId = contextoCiclo is null ? null : (await contextoCiclo.ObtenerAsync()).CicloEscolarId;
        var autorizada = await contexto.Tareas.AsNoTracking().AnyAsync(t => t.Id == tareaId &&
            t.Estado == EstadoTarea.Publicada && (!t.FechaDisponibilidad.HasValue || t.FechaDisponibilidad <= ahora) &&
            (cicloId.HasValue
                ? t.Asignacion.Seccion.CicloEscolarId == cicloId.Value
                : t.Asignacion.Seccion.CicloEscolar.Activo) &&
            (t.Asignacion.Estado == EstadoRegistro.Activo || contexto.DelegacionesAcademicas.Any(d =>
                d.AsignacionId == t.AsignacionId && d.Activa && d.FechaInicioUtc <= ahora &&
                (!d.FechaFinUtc.HasValue || d.FechaFinUtc >= ahora) &&
                (d.Operaciones & OperacionDelegacionAcademica.ContinuarActividadesAlumnos) == OperacionDelegacionAcademica.ContinuarActividadesAlumnos)) &&
            contexto.Inscripciones.Any(i => i.AlumnoId == alumnoId.Value && i.SeccionId == t.Asignacion.SeccionId &&
                i.CicloEscolarId == t.Asignacion.Seccion.CicloEscolarId && i.Estado == EstadoInscripcion.Activa), ct);
        if (!autorizada || await contexto.VisualizacionesActividades.AnyAsync(x => x.AlumnoId == alumnoId.Value && x.TareaId == tareaId, ct)) return;
        await using var tx = await contexto.Database.BeginTransactionAsync(ct);
        try
        {
            contexto.VisualizacionesActividades.Add(new VisualizacionActividad { AlumnoId = alumnoId.Value, TareaId = tareaId, FechaVisualizacionUtc = ahora });
            await contexto.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync(ct);
            contexto.ChangeTracker.Clear(); // la clave compuesta absorbió la carrera de dos pestañas
        }
    }

    private async Task AgregarAlumnoAsync(ResumenNotificaciones r, int alumnoId, DateTime ahora, int? cicloId)
    {
        if (acceso.TienePermiso(Permisos.Entregas.Ver) || acceso.TienePermiso(Permisos.Cuestionarios.Ver))
        {
            var nuevas = await contexto.Tareas.AsNoTracking().Where(t => t.Estado == EstadoTarea.Publicada &&
                (!t.FechaDisponibilidad.HasValue || t.FechaDisponibilidad <= ahora) &&
                (cicloId.HasValue ? t.Asignacion.Seccion.CicloEscolarId == cicloId.Value : t.Asignacion.Seccion.CicloEscolar.Activo) &&
                (t.Asignacion.Estado == EstadoRegistro.Activo || contexto.DelegacionesAcademicas.Any(d => d.AsignacionId == t.AsignacionId && d.Activa && d.FechaInicioUtc <= ahora && (!d.FechaFinUtc.HasValue || d.FechaFinUtc >= ahora) && (d.Operaciones & OperacionDelegacionAcademica.ContinuarActividadesAlumnos) == OperacionDelegacionAcademica.ContinuarActividadesAlumnos)) &&
                contexto.Inscripciones.Any(i => i.AlumnoId == alumnoId && i.SeccionId == t.Asignacion.SeccionId && i.CicloEscolarId == t.Asignacion.Seccion.CicloEscolarId && i.Estado == EstadoInscripcion.Activa) &&
                !contexto.VisualizacionesActividades.Any(v => v.AlumnoId == alumnoId && v.TareaId == t.Id))
                .OrderByDescending(t => t.FechaPublicacion).Select(t => new
                {
                    t.Titulo,
                    t.Tipo,
                    t.FechaPublicacion,
                    Curso = t.Asignacion.Curso.Nombre,
                    CuestionarioId = t.Cuestionario == null ? (int?)null : t.Cuestionario.Id,
                    EntregaId = t.Entregas.Where(e => e.AlumnoId == alumnoId && e.Estado == EstadoEntrega.Pendiente).Select(e => (int?)e.Id).FirstOrDefault()
                }).ToListAsync();
            foreach (var x in nuevas)
            {
                var cuestionario = x.Tipo == TipoTarea.Cuestionario;
                if (cuestionario && (!x.CuestionarioId.HasValue || !acceso.TienePermiso(Permisos.Cuestionarios.Resolver))) continue;
                if (!cuestionario && (!x.EntregaId.HasValue || !acceso.TienePermiso(Permisos.Entregas.Entregar))) continue;
                r.Notificaciones.Add(new NotificacionLista
                {
                    Titulo = cuestionario ? "Nuevo cuestionario publicado" : "Nueva tarea publicada",
                    Mensaje = $"{x.Titulo} · {x.Curso}.",
                    Icono = cuestionario ? "bi bi-ui-checks" : "bi bi-journal-plus",
                    Tipo = "info",
                    Controlador = cuestionario ? "Cuestionarios" : "Entregas",
                    Accion = cuestionario ? "Resolver" : "Entregar",
                    Id = cuestionario ? x.CuestionarioId : x.EntregaId,
                    Fecha = x.FechaPublicacion,
                    EsNovedad = true
                });
            }
        }
        if (!acceso.TienePermiso(Permisos.Entregas.Ver)) return;
        var vigentes = EntregasVigentesAlumno(alumnoId, ahora, cicloId);
        var pendientes = await vigentes.CountAsync(x => x.Estado == EstadoEntrega.Pendiente);
        if (pendientes > 0) r.Notificaciones.Add(Alerta("Tareas pendientes", $"Tiene {pendientes} tarea(s) pendientes de entrega.", "bi bi-list-check", "warning", "Entregas", ahora));
        var proximas = await EntregasVigentesAlumno(alumnoId, ahora, cicloId).CountAsync(x => x.Estado == EstadoEntrega.Pendiente &&
            (x.FechaLimiteIndividual ?? x.Tarea.FechaLimite) >= ahora && (x.FechaLimiteIndividual ?? x.Tarea.FechaLimite) <= ahora.AddDays(2));
        if (proximas > 0) r.Notificaciones.Add(Alerta("Próximas a vencer", $"{proximas} tarea(s) vencen durante las próximas 48 horas.", "bi bi-clock", "danger", "Entregas", ahora));
    }

    private IQueryable<Entrega> EntregasVigentesAlumno(int alumnoId, DateTime ahora, int? cicloId) => contexto.Entregas.AsNoTracking().Where(x =>
        x.AlumnoId == alumnoId &&
        (cicloId.HasValue ? x.Tarea.Asignacion.Seccion.CicloEscolarId == cicloId.Value : x.Tarea.Asignacion.Seccion.CicloEscolar.Activo) && contexto.Inscripciones.Any(i => i.AlumnoId == alumnoId && i.SeccionId == x.Tarea.Asignacion.SeccionId && i.CicloEscolarId == x.Tarea.Asignacion.Seccion.CicloEscolarId && i.Estado == EstadoInscripcion.Activa) &&
        !contexto.Entregas.Any(o => o.TareaId == x.TareaId && o.AlumnoId == x.AlumnoId && o.InscripcionId == x.InscripcionId && o.NumeroEnvio > x.NumeroEnvio));

    private async Task AgregarDocenteAsync(ResumenNotificaciones r, int docenteId, DateTime ahora, int? cicloId)
    {
        if (acceso.TienePermiso(Permisos.Entregas.Ver) && acceso.TienePermiso(Permisos.Entregas.Calificar))
        {
            var total = await contexto.Entregas.CountAsync(x => (x.Tarea.Asignacion.DocenteId == docenteId || contexto.DelegacionesAcademicas.Any(d => d.AsignacionId == x.Tarea.AsignacionId && d.DocenteId == docenteId && d.Activa && d.FechaInicioUtc <= ahora && (!d.FechaFinUtc.HasValue || d.FechaFinUtc >= ahora) && (d.Operaciones & OperacionDelegacionAcademica.CalificarEntregas) == OperacionDelegacionAcademica.CalificarEntregas)) && (cicloId.HasValue ? x.Tarea.Asignacion.Seccion.CicloEscolarId == cicloId.Value : x.Tarea.Asignacion.Seccion.CicloEscolar.Activo) && (x.Estado == EstadoEntrega.Enviada || x.Estado == EstadoEntrega.Tardia) && !contexto.Entregas.Any(o => o.TareaId == x.TareaId && o.AlumnoId == x.AlumnoId && o.InscripcionId == x.InscripcionId && o.NumeroEnvio > x.NumeroEnvio));
            if (total > 0) r.Notificaciones.Add(Alerta("Entregas por revisar", $"Tiene {total} entrega(s) pendientes de revisión.", "bi bi-inbox", "warning", "Entregas", ahora));
        }
        if (acceso.TienePermiso(Permisos.Planificaciones.Ver))
        {
            var total = await contexto.Planificaciones.CountAsync(x => x.DocenteId == docenteId && x.Estado == EstadoPlanificacion.Devuelta && x.Asignaciones.Any(a => cicloId.HasValue ? a.Asignacion.Seccion.CicloEscolarId == cicloId.Value : a.Asignacion.Seccion.CicloEscolar.Activo));
            if (total > 0) r.Notificaciones.Add(Alerta("Planificaciones devueltas", $"Tiene {total} planificación(es) con observaciones.", "bi bi-clipboard2-x", "danger", "Planificaciones", ahora));
        }
        if (!acceso.TienePermiso(Permisos.Asignaciones.Ver)) return;
        var relevos = await contexto.RelevosAsignaciones.AsNoTracking().Where(x => (x.DocenteAnteriorId == docenteId || x.DocenteSustitutoId == docenteId) && x.FechaRegistroUtc >= ahora.AddDays(-30) && (cicloId.HasValue ? x.AsignacionNueva.Seccion.CicloEscolarId == cicloId.Value : x.AsignacionNueva.Seccion.CicloEscolar.Activo)).OrderByDescending(x => x.FechaRegistroUtc).Take(5).Select(x => new { x.AsignacionNuevaId, x.Tipo, x.FechaRegistroUtc, Curso = x.AsignacionNueva.Curso.Nombre, Seccion = x.AsignacionNueva.Seccion.Nombre, EsSustituto = x.DocenteSustitutoId == docenteId }).ToListAsync();
        foreach (var x in relevos) r.Notificaciones.Add(new NotificacionLista { Titulo = x.EsSustituto ? "Nueva asignación por relevo" : "Asignación finalizada por relevo", Mensaje = $"{x.Curso} · sección {x.Seccion} ({x.Tipo}).", Icono = "bi bi-arrow-left-right", Tipo = x.Tipo == TipoRelevoAsignacion.Excepcional ? "warning" : "info", Controlador = "Asignaciones", Accion = "Index", Id = x.AsignacionNuevaId, Fecha = x.FechaRegistroUtc });
    }

    private static NotificacionLista Alerta(string titulo, string mensaje, string icono, string tipo, string controlador, DateTime fecha) => new()
    { Titulo = titulo, Mensaje = mensaje, Icono = icono, Tipo = tipo, Controlador = controlador, Accion = "Index", Fecha = fecha };
}

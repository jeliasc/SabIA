using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Notificaciones;

namespace Proyecto_Final.Servicios.GestionNotificaciones;

public sealed class NotificacionServicio(
    Contexto c,
    IAccesoAcademicoServicio acceso) : INotificacionServicio
{
    public async Task<List<NotificacionLista>> ObtenerAsync()
    {
        var r = new List<NotificacionLista>();

        var d = await acceso.ObtenerDocenteIdAsync();
        var a = await acceso.ObtenerAlumnoIdAsync();
        var ahora = DateTime.UtcNow;

        if (d.HasValue)
        {
            var entregas = await c.Entregas.CountAsync(x =>
                x.Tarea.Asignacion.DocenteId == d.Value &&
                x.Tarea.Asignacion.Seccion.CicloEscolar.Activo &&
                (
                    x.Estado == EstadoEntrega.Enviada ||
                    x.Estado == EstadoEntrega.Tardia
                ) &&
                !c.Entregas.Any(otra =>
                    otra.TareaId == x.TareaId &&
                    otra.AlumnoId == x.AlumnoId &&
                    otra.NumeroEnvio > x.NumeroEnvio)
            );

            if (entregas > 0)
            {
                r.Add(
                    new NotificacionLista
                    {
                        Titulo = "Entregas por revisar",
                        Mensaje = $"Tiene {entregas} entrega(s) pendientes de revisión.",
                        Icono = "bi bi-inbox",
                        Tipo = "warning",
                        Controlador = "Entregas",
                        Accion = "Index"
                    }
                );
            }

            var planes = await c.Planificaciones.CountAsync(x =>
                x.DocenteId == d.Value &&
                x.Estado == EstadoPlanificacion.Devuelta &&
                x.Asignaciones.Any(a =>
                    a.Asignacion.Seccion.CicloEscolar.Activo)
            );

            if (planes > 0)
            {
                r.Add(
                    new NotificacionLista
                    {
                        Titulo = "Planificaciones devueltas",
                        Mensaje = $"Tiene {planes} planificación(es) con observaciones.",
                        Icono = "bi bi-clipboard2-x",
                        Tipo = "danger",
                        Controlador = "Planificaciones",
                        Accion = "Index"
                    }
                );
            }
        }
        else if (a.HasValue)
        {
            var pendientes = await c.Entregas.CountAsync(x =>
                x.AlumnoId == a.Value &&
                x.Estado == EstadoEntrega.Pendiente &&
                (x.Tarea.Estado == EstadoTarea.Publicada ||
                 x.FechaLimiteIndividual >= ahora) &&
                c.Inscripciones.Any(inscripcion =>
                    inscripcion.AlumnoId == a.Value &&
                    inscripcion.SeccionId == x.Tarea.Asignacion.SeccionId &&
                    inscripcion.Estado == EstadoInscripcion.Activa &&
                    inscripcion.Seccion.CicloEscolar.Activo) &&
                !c.Entregas.Any(otra =>
                    otra.TareaId == x.TareaId &&
                    otra.AlumnoId == x.AlumnoId &&
                    otra.NumeroEnvio > x.NumeroEnvio)
            );

            if (pendientes > 0)
            {
                r.Add(
                    new NotificacionLista
                    {
                        Titulo = "Tareas pendientes",
                        Mensaje = $"Tiene {pendientes} tarea(s) pendientes de entrega.",
                        Icono = "bi bi-list-check",
                        Tipo = "warning",
                        Controlador = "Entregas",
                        Accion = "Index"
                    }
                );
            }

            var proximas = await c.Entregas.CountAsync(x =>
                x.AlumnoId == a.Value &&
                x.Estado == EstadoEntrega.Pendiente &&
                (x.FechaLimiteIndividual ?? x.Tarea.FechaLimite) >= ahora &&
                (x.FechaLimiteIndividual ?? x.Tarea.FechaLimite) <= ahora.AddDays(2) &&
                c.Inscripciones.Any(inscripcion =>
                    inscripcion.AlumnoId == a.Value &&
                    inscripcion.SeccionId == x.Tarea.Asignacion.SeccionId &&
                    inscripcion.Estado == EstadoInscripcion.Activa &&
                    inscripcion.Seccion.CicloEscolar.Activo) &&
                !c.Entregas.Any(otra =>
                    otra.TareaId == x.TareaId &&
                    otra.AlumnoId == x.AlumnoId &&
                    otra.NumeroEnvio > x.NumeroEnvio)
            );

            if (proximas > 0)
            {
                r.Add(
                    new NotificacionLista
                    {
                        Titulo = "Próximas a vencer",
                        Mensaje = $"{proximas} tarea(s) vencen durante las próximas 48 horas.",
                        Icono = "bi bi-clock",
                        Tipo = "danger",
                        Controlador = "Entregas",
                        Accion = "Index"
                    }
                );
            }
        }
        else
        {
            var revision = await c.Planificaciones.CountAsync(x =>
                x.Estado == EstadoPlanificacion.EnRevision &&
                x.Asignaciones.Any(a =>
                    a.Asignacion.Seccion.CicloEscolar.Activo)
            );

            if (revision > 0)
            {
                r.Add(
                    new NotificacionLista
                    {
                        Titulo = "Planificaciones en revisión",
                        Mensaje = $"Hay {revision} planificación(es) pendientes de revisión.",
                        Icono = "bi bi-clipboard2-check",
                        Tipo = "warning",
                        Controlador = "Planificaciones",
                        Accion = "Index"
                    }
                );
            }
        }

        if (r.Count == 0)
        {
            r.Add(
                new NotificacionLista
                {
                    Titulo = "Sin pendientes importantes",
                    Mensaje = "No se encontraron alertas académicas para su cuenta.",
                    Icono = "bi bi-check2-circle",
                    Tipo = "success"
                }
            );
        }

        return r;
    }
}

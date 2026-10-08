using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;

namespace Proyecto_Final.Servicios.Academico;

public sealed class EntregaPendienteServicio(Contexto contexto)
    : IEntregaPendienteServicio
{
    public async Task CrearParaTareasDisponiblesAsync(
        int alumnoId,
        int seccionId,
        CancellationToken cancellationToken = default)
    {
        var inscripcionId = await contexto.Inscripciones
            .AsNoTracking()
            .Where(x =>
                x.AlumnoId == alumnoId &&
                x.SeccionId == seccionId &&
                x.Estado == EstadoInscripcion.Activa &&
                x.Seccion.CicloEscolar.Activo)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!inscripcionId.HasValue)
        {
            return;
        }

        var tareas = await contexto.Tareas
            .AsNoTracking()
            .Where(tarea =>
                tarea.Asignacion.SeccionId == seccionId &&
                tarea.Asignacion.Estado == EstadoRegistro.Activo &&
                tarea.Asignacion.Seccion.CicloEscolar.Activo &&
                tarea.Estado == EstadoTarea.Publicada &&
                !contexto.Entregas.Any(entrega =>
                    entrega.TareaId == tarea.Id &&
                    entrega.InscripcionId == inscripcionId.Value))
            .Select(tarea => tarea.Id)
            .ToListAsync(cancellationToken);

        contexto.Entregas.AddRange(tareas.Select(tareaId => new Entrega
        {
            TareaId = tareaId,
            AlumnoId = alumnoId,
            InscripcionId = inscripcionId.Value,
            NumeroEnvio = 1,
            Estado = EstadoEntrega.Pendiente
        }));
    }
}

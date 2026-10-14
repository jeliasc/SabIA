using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;

namespace Proyecto_Final.Servicios.GestionAsignaciones;

public interface IDelegacionAcademicaServicio
{
    Task<bool> PuedeActuarAsync(
        int asignacionId,
        int docenteId,
        OperacionDelegacionAcademica operacion);
}

public sealed class DelegacionAcademicaServicio(Contexto contexto)
    : IDelegacionAcademicaServicio
{
    public Task<bool> PuedeActuarAsync(
        int asignacionId,
        int docenteId,
        OperacionDelegacionAcademica operacion)
    {
        var ahora = DateTime.UtcNow;
        return contexto.DelegacionesAcademicas.AsNoTracking().AnyAsync(x =>
            x.AsignacionId == asignacionId &&
            x.DocenteId == docenteId &&
            x.Activa &&
            x.FechaInicioUtc <= ahora &&
            (!x.FechaFinUtc.HasValue || x.FechaFinUtc >= ahora) &&
            (x.Operaciones & operacion) == operacion);
    }
}

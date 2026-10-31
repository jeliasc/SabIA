using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;

namespace Proyecto_Final.Servicios.GestionCalificaciones;

public enum EstadoResultadoOficialTraslado
{
    Ordinario,
    PendienteResolucion,
    FuenteOficial,
    ConsolidacionPendiente,
    ConsolidacionOficial
}

/// <summary>Selección de fuente oficial; nunca copia ni recalcula evidencias.</summary>
public sealed record ResultadoOficialTraslado(
    EstadoResultadoOficialTraslado Estado,
    int? ResolucionId = null,
    int? AsignacionFuenteId = null,
    decimal? NotaConsolidada = null,
    decimal? EscalaMaximaConsolidada = null);

public interface IResultadoOficialTrasladoServicio
{
    Task<IReadOnlyDictionary<int, ResultadoOficialTraslado>> ObtenerAsync(
        IEnumerable<int> inscripcionesIds, int cursoId, Periodo periodo);
}

public sealed class ResultadoOficialTrasladoServicio(Contexto contexto) : IResultadoOficialTrasladoServicio
{
    public async Task<IReadOnlyDictionary<int, ResultadoOficialTraslado>> ObtenerAsync(
        IEnumerable<int> inscripcionesIds, int cursoId, Periodo periodo)
    {
        var ids = inscripcionesIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, ResultadoOficialTraslado>();

        // Las resoluciones anteriores se conservan como evidencia institucional,
        // pero ya no seleccionan ni bloquean la fuente ordinaria: ésta es el
        // libro manual de la sección vigente. Los cierres ya guardan su propia
        // instantánea de resultados.
        return ids.ToDictionary(x => x,
            _ => new ResultadoOficialTraslado(EstadoResultadoOficialTraslado.Ordinario));

#pragma warning disable CS0162 // Código histórico conservado para compatibilidad documental.
        var resoluciones = await contexto.ResolucionesTrasladosAcademicos.AsNoTracking()
            .Where(x => ids.Contains(x.InscripcionId) && x.CursoId == cursoId &&
                x.PeriodoId == periodo.Id && x.Estado == EstadoResolucionTraslado.Aprobada && x.Vigente)
            .Include(x => x.Fuentes).ToListAsync();
        var porInscripcion = resoluciones.ToDictionary(x => x.InscripcionId);
        var traslados = await contexto.HistorialTraslados.AsNoTracking()
            .Where(x => ids.Contains(x.InscripcionId) && x.CicloEscolarId == periodo.CicloEscolarId)
            .ToListAsync();
        var conTrasladoEnPeriodo = traslados
            .Where(x => PertenenciaAcademicaHistoricaServicio.FechaAcademica(x) >= periodo.FechaInicio &&
                        PertenenciaAcademicaHistoricaServicio.FechaAcademica(x) <= periodo.FechaFin)
            .Select(x => x.InscripcionId).ToHashSet();
        var resultado = new Dictionary<int, ResultadoOficialTraslado>();
        foreach (var inscripcionId in ids)
        {
            if (!conTrasladoEnPeriodo.Contains(inscripcionId))
            {
                resultado[inscripcionId] = new ResultadoOficialTraslado(EstadoResultadoOficialTraslado.Ordinario);
                continue;
            }
            if (!porInscripcion.TryGetValue(inscripcionId, out var resolucion))
            {
                resultado[inscripcionId] = new ResultadoOficialTraslado(EstadoResultadoOficialTraslado.PendienteResolucion);
                continue;
            }
            if (resolucion.Tipo == TipoResolucionTraslado.Consolidacion)
            {
                resultado[inscripcionId] = new ResultadoOficialTraslado(
                    resolucion.NotaConsolidadaAprobada.HasValue && resolucion.EscalaMaximaConsolidada > 0
                        ? EstadoResultadoOficialTraslado.ConsolidacionOficial
                        : EstadoResultadoOficialTraslado.ConsolidacionPendiente,
                    resolucion.Id, null, resolucion.NotaConsolidadaAprobada,
                    resolucion.EscalaMaximaConsolidada);
                continue;
            }

            var rol = resolucion.Tipo == TipoResolucionTraslado.Origen ? "Origen" : "Destino";
            var fuente = resolucion.Fuentes.FirstOrDefault(x =>
                string.Equals(x.Rol, rol, StringComparison.OrdinalIgnoreCase));
            resultado[inscripcionId] = fuente is null
                ? new ResultadoOficialTraslado(EstadoResultadoOficialTraslado.PendienteResolucion, resolucion.Id)
                : new ResultadoOficialTraslado(EstadoResultadoOficialTraslado.FuenteOficial,
                    resolucion.Id, fuente.AsignacionId);
        }
        return resultado;
#pragma warning restore CS0162
    }
}

using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Comunes;

namespace Proyecto_Final.Servicios.GestionCalificaciones;

/// <summary>
/// Reconstruye la sección académica de una inscripción para un período sin
/// modificar la inscripción vigente ni el historial de traslados.
/// </summary>
public interface IPertenenciaAcademicaHistoricaServicio
{
    Task<IReadOnlyDictionary<int, TipoPertenenciaLibro>> ObtenerParaSeccionAsync(
        ConfiguracionEvaluacion configuracion, bool soloActivas);

    Task<bool> TieneTrasladoEnPeriodoAsync(int inscripcionId, Periodo periodo);
}

public enum TipoPertenenciaLibro
{
    MatriculaOrdinaria,
    EvidenciaHistorica
}

public sealed class PertenenciaAcademicaHistoricaServicio(Contexto contexto)
    : IPertenenciaAcademicaHistoricaServicio
{
    public async Task<IReadOnlyDictionary<int, TipoPertenenciaLibro>> ObtenerParaSeccionAsync(
        ConfiguracionEvaluacion configuracion, bool soloActivas)
    {
        var consulta = contexto.Inscripciones.AsNoTracking()
            .Where(x => x.CicloEscolarId == configuracion.CicloEscolarId);
        if (soloActivas)
            consulta = consulta.Where(x => x.Estado == EstadoInscripcion.Activa);

        var inscripciones = await consulta.ToListAsync();
        if (inscripciones.Count == 0) return new Dictionary<int, TipoPertenenciaLibro>();

        var ids = inscripciones.Select(x => x.Id).ToList();
        var traslados = await contexto.HistorialTraslados.AsNoTracking()
            .Where(x => x.CicloEscolarId == configuracion.CicloEscolarId && ids.Contains(x.InscripcionId))
            .OrderBy(x => x.FechaTraslado).ThenBy(x => x.Id).ToListAsync();
        var porInscripcion = traslados.GroupBy(x => x.InscripcionId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<HistorialTraslado>)x.ToList());

        var resultado = new Dictionary<int, TipoPertenenciaLibro>();
        foreach (var inscripcion in inscripciones)
        {
            var historial = porInscripcion.GetValueOrDefault(inscripcion.Id) ?? [];
            var pertenencia = DeterminarPertenencia(inscripcion, historial, configuracion);
            if (pertenencia.HasValue)
                resultado[inscripcion.Id] = pertenencia.Value;
        }
        return resultado;
    }

    public async Task<bool> TieneTrasladoEnPeriodoAsync(int inscripcionId, Periodo periodo)
    {
        var traslados = await contexto.HistorialTraslados.AsNoTracking()
            .Where(x => x.InscripcionId == inscripcionId && x.CicloEscolarId == periodo.CicloEscolarId)
            .ToListAsync();
        return traslados.Any(x => FechaAcademica(x) >= periodo.FechaInicio &&
                                  FechaAcademica(x) <= periodo.FechaFin);
    }

    private static TipoPertenenciaLibro? DeterminarPertenencia(
        Inscripcion inscripcion,
        IReadOnlyList<HistorialTraslado> traslados,
        ConfiguracionEvaluacion configuracion)
    {
        if (traslados.Count == 0)
            return inscripcion.SeccionId == configuracion.SeccionId
                ? TipoPertenenciaLibro.MatriculaOrdinaria : null;

        // Un historial incompleto no se interpreta: es preferible no atribuir una
        // matrícula académica antes que mostrarla en una sección equivocada.
        var seccionEsperada = traslados[0].SeccionOrigenId;
        foreach (var traslado in traslados)
        {
            if (traslado.SeccionOrigenId != seccionEsperada ||
                FechaAcademica(traslado) < inscripcion.Fecha)
                return null;
            seccionEsperada = traslado.SeccionDestinoId;
        }
        if (seccionEsperada != inscripcion.SeccionId) return null;

        // La matrícula ordinaria se decide al cierre civil del período, no por
        // la sección actual ni por todo el historial. Así, un traslado A -> B
        // dentro del bimestre deja B como libro editable de ese bimestre y de
        // los posteriores, sin atribuir B a períodos anteriores.
        var seccionAlFinDelPeriodo = traslados[0].SeccionOrigenId;
        foreach (var traslado in traslados)
        {
            if (FechaAcademica(traslado) > configuracion.Periodo.FechaFin)
                break;
            seccionAlFinDelPeriodo = traslado.SeccionDestinoId;
        }
        if (seccionAlFinDelPeriodo == configuracion.SeccionId)
            return TipoPertenenciaLibro.MatriculaOrdinaria;

        // Una sección sólo puede ser candidata a evidencia histórica si el
        // alumno realmente perteneció a ella en algún día inclusivo del período.
        // CalificacionServicio descarta después los candidatos sin una nota o
        // evidencia real de ese libro, evitando filas históricas ficticias.
        var inicioTramo = inscripcion.Fecha;
        var seccionTramo = traslados[0].SeccionOrigenId;
        var tieneEvidenciaPosible = false;
        foreach (var traslado in traslados)
        {
            var finTramo = FechaAcademica(traslado).AddDays(-1);
            if (seccionTramo == configuracion.SeccionId &&
                inicioTramo <= configuracion.Periodo.FechaFin &&
                finTramo >= configuracion.Periodo.FechaInicio)
            {
                tieneEvidenciaPosible = true;
                break;
            }
            inicioTramo = FechaAcademica(traslado);
            seccionTramo = traslado.SeccionDestinoId;
        }
        if (!tieneEvidenciaPosible && seccionTramo == configuracion.SeccionId &&
            inicioTramo <= configuracion.Periodo.FechaFin)
        {
            tieneEvidenciaPosible = true;
        }
        return tieneEvidenciaPosible ? TipoPertenenciaLibro.EvidenciaHistorica : null;
    }

    internal static DateOnly FechaAcademica(HistorialTraslado traslado) =>
        traslado.FechaEfectiva ?? DateOnly.FromDateTime(HoraGuatemala.ParaFormulario(traslado.FechaTraslado));
}

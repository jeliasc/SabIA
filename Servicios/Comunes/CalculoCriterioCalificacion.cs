using Proyecto_Final.Models;

namespace Proyecto_Final.Servicios.Comunes;

// Único cálculo para las notas efectivas de intentos de archivo. El criterio
// nulo sólo existe en tareas históricas y equivale al último intento calificado.
public static class CalculoCriterioCalificacion
{
    public static decimal? Aplicar(
        CriterioCalificacionIntentos? criterio,
        IEnumerable<(int NumeroEnvio, int Id, decimal? Calificacion)> intentos) =>
        Aplicar(criterio, intentos.Select(x =>
            (x.NumeroEnvio, x.Id, x.Calificacion, false)));

    public static decimal? Aplicar(
        CriterioCalificacionIntentos? criterio,
        IEnumerable<(int NumeroEnvio, int Id, decimal? Calificacion,
            bool EsCalificacionAdministrativa)> intentos)
    {
        var calificadas = intentos
            .Where(x => x.Calificacion.HasValue)
            .OrderBy(x => x.NumeroEnvio)
            .ThenBy(x => x.Id)
            .ToList();
        var notas = calificadas
            // El cero administrativo no representa un intento académico y no
            // puede entrar en mejor nota, último intento ni promedio.
            .Where(x => !x.EsCalificacionAdministrativa)
            .Select(x => x.Calificacion!.Value)
            .ToList();
        if (notas.Count == 0)
        {
            // Mientras no exista un intento académico, el cero administrativo
            // sigue siendo la nota efectiva de la actividad. Al aparecer una
            // entrega/intentó válido se elimina del conjunto de cálculo.
            return calificadas
                .Where(x => x.EsCalificacionAdministrativa)
                .Select(x => x.Calificacion)
                .LastOrDefault();
        }

        return (criterio ?? CriterioCalificacionIntentos.UltimoIntento) switch
        {
            CriterioCalificacionIntentos.MejorNota => notas.Max(),
            CriterioCalificacionIntentos.Promedio => Math.Round(notas.Average(), 2),
            _ => notas[^1]
        };
    }
}

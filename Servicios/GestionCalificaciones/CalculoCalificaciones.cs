using Proyecto_Final.Models;

namespace Proyecto_Final.Servicios.GestionCalificaciones;

public static class CalculoCalificaciones
{
    public static ResultadoCalculoCalificacion Calcular(
        MetodoCalculoEvaluacion metodo,
        IReadOnlyCollection<CategoriaCalculoCalificacion> categorias)
    {
        if (categorias.Count == 0)
            throw new ArgumentException("Debe existir al menos una categoría.", nameof(categorias));

        if (categorias.Any(x => x.Porcentaje <= 0 || x.Porcentaje > 100))
            throw new ArgumentException("Las ponderaciones deben estar entre 0 y 100.", nameof(categorias));

        if (categorias.Sum(x => x.Porcentaje) != 100m)
            throw new ArgumentException("Las ponderaciones deben sumar 100.", nameof(categorias));

        if (categorias.Any(x => x.Actividades.Count == 0))
            throw new ArgumentException("Cada categoría debe contener actividades.", nameof(categorias));

        foreach (var actividad in categorias.SelectMany(x => x.Actividades))
        {
            if (actividad.PunteoMaximo <= 0)
                throw new ArgumentException("El punteo máximo debe ser mayor que cero.", nameof(categorias));

            if (actividad.Nota.HasValue &&
                (actividad.Nota.Value < 0 || actividad.Nota.Value > actividad.PunteoMaximo))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(categorias),
                    $"La nota de {actividad.Nombre} está fuera del rango permitido.");
            }
        }

        var pendientes = categorias
            .SelectMany(x => x.Actividades)
            .Count(x => !x.Nota.HasValue);

        decimal TotalTipo(TipoCategoriaEvaluacion tipo) => categorias
            .Where(x => x.Tipo == tipo)
            .SelectMany(x => x.Actividades)
            .Sum(x => x.Nota ?? 0m);

        decimal MaximoTipo(TipoCategoriaEvaluacion tipo) => categorias
            .Where(x => x.Tipo == tipo)
            .SelectMany(x => x.Actividades)
            .Sum(x => x.PunteoMaximo);

        var desempeno = TotalTipo(TipoCategoriaEvaluacion.Desempeno);
        var actitudinal = TotalTipo(TipoCategoriaEvaluacion.Actitudinal);
        var maximoDesempeno = MaximoTipo(TipoCategoriaEvaluacion.Desempeno);
        var maximoActitudinal = MaximoTipo(TipoCategoriaEvaluacion.Actitudinal);

        var notaBimestral = metodo == MetodoCalculoEvaluacion.SumaPuntos
            ? categorias.SelectMany(x => x.Actividades).Sum(x => x.Nota ?? 0m)
            : categorias.Sum(categoria =>
            {
                var maximo = categoria.Actividades.Sum(x => x.PunteoMaximo);
                var obtenido = categoria.Actividades.Sum(x => x.Nota ?? 0m);
                return maximo == 0m ? 0m : obtenido * categoria.Porcentaje / maximo;
            });

        return new ResultadoCalculoCalificacion(
            desempeno,
            actitudinal,
            notaBimestral,
            maximoDesempeno == 0m ? 0m : desempeno * 100m / maximoDesempeno,
            maximoActitudinal == 0m ? 0m : actitudinal * 100m / maximoActitudinal,
            pendientes);
    }
}

public sealed record CategoriaCalculoCalificacion(
    TipoCategoriaEvaluacion Tipo,
    decimal Porcentaje,
    IReadOnlyCollection<ActividadCalculoCalificacion> Actividades);

public sealed record ActividadCalculoCalificacion(
    string Nombre,
    decimal PunteoMaximo,
    decimal? Nota);

public sealed record ResultadoCalculoCalificacion(
    decimal Desempeno,
    decimal Actitudinal,
    decimal NotaBimestral,
    decimal AbacusDesempeno,
    decimal AbacusActitudinal,
    int Pendientes);

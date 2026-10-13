using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Final.Models;

public class CategoriaEvaluacion
{
    public int Id { get; set; }

    public int ConfiguracionEvaluacionId { get; set; }

    public ConfiguracionEvaluacion ConfiguracionEvaluacion { get; set; } =
        null!;

    [Required, StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    public TipoCategoriaEvaluacion Tipo { get; set; } =
        TipoCategoriaEvaluacion.Otra;

    [Precision(5, 2)]
    public decimal Porcentaje { get; set; }

    [Range(1, int.MaxValue)]
    public int Orden { get; set; }

    public ICollection<ActividadEvaluable> Actividades { get; set; } = new List<ActividadEvaluable>();
}

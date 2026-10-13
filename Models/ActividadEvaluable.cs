using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Final.Models;

public class ActividadEvaluable
{
    public int Id { get; set; }

    public int ConfiguracionEvaluacionId { get; set; }
    public int AsignacionId { get; set; }

    public ConfiguracionEvaluacion ConfiguracionEvaluacion { get; set; } =
        null!;

    public int? CategoriaEvaluacionId { get; set; }

    public CategoriaEvaluacion? CategoriaEvaluacion { get; set; }

    public OrigenActividadEvaluable Origen { get; set; }

    public int? TareaId { get; set; }

    public Tarea? Tarea { get; set; }

    [Required, StringLength(200)]
    public string Nombre { get; set; } = string.Empty;

    [Precision(9, 2)]
    public decimal PunteoMaximo { get; set; }

    [Range(1, int.MaxValue)]
    public int Orden { get; set; }

    public bool Activa { get; set; } = true;

    public DateTime FechaCreacion { get; set; }

    public ICollection<CalificacionManual> CalificacionesManuales { get; set; } = new List<CalificacionManual>();
}
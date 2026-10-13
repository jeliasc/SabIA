using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public class ConfiguracionEvaluacion
{
    public int Id { get; set; }

    public int AsignacionId { get; set; }
    public int SeccionId { get; set; }
    public int CicloEscolarId { get; set; }
    public Asignacion Asignacion { get; set; } = null!;

    public int PeriodoId { get; set; }
    public Periodo Periodo { get; set; } = null!;

    public MetodoCalculoEvaluacion MetodoCalculo { get; set; } = MetodoCalculoEvaluacion.SumaPuntos;

    public EstadoConfiguracionEvaluacion Estado { get; set; } = EstadoConfiguracionEvaluacion.Borrador;

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaActivacion { get; set; }

    [Required]
    public string ConfiguradoPorUsuarioId { get; set; } = string.Empty;

    public Usuario ConfiguradoPorUsuario { get; set; } = null!;

    public ICollection<CategoriaEvaluacion> Categorias { get; set; } = new List<CategoriaEvaluacion>();

    public ICollection<ActividadEvaluable> Actividades { get; set; } = new List<ActividadEvaluable>();
}
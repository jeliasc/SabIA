using System.ComponentModel.DataAnnotations;
using Proyecto_Final.Models;
using Proyecto_Final.ViewModels.Comunes;

namespace Proyecto_Final.ViewModels.Calificaciones;

public sealed class ConfigurarEvaluacion
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int AsignacionId { get; set; }

    [Range(1, int.MaxValue)]
    public int PeriodoId { get; set; }

    public MetodoCalculoEvaluacion MetodoCalculo { get; set; } =
        MetodoCalculoEvaluacion.SumaPuntos;

    public int? PlantillaId { get; set; }

    public bool GuardarComoPlantilla { get; set; }

    [StringLength(150)]
    public string? NombrePlantilla { get; set; }

    public List<CategoriaEvaluacionFormulario> Categorias { get; set; } = [];
    public List<OpcionSeleccion> Asignaciones { get; set; } = [];
    public List<OpcionSeleccion> Periodos { get; set; } = [];
    public List<OpcionSeleccion> Tareas { get; set; } = [];
    public List<OpcionSeleccion> Plantillas { get; set; } = [];
}

public sealed class CategoriaEvaluacionFormulario
{
    [Required, StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    public TipoCategoriaEvaluacion Tipo { get; set; }

    [Range(typeof(decimal), "0.01", "100")]
    public decimal Porcentaje { get; set; }

    public List<ActividadEvaluableFormulario> Actividades { get; set; } = [];
}

public sealed class ActividadEvaluableFormulario
{
    [Required, StringLength(200)]
    public string Nombre { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999")]
    public decimal PunteoMaximo { get; set; }

    public int? TareaId { get; set; }
}

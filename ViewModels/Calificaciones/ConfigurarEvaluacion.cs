using System.ComponentModel.DataAnnotations;
using Proyecto_Final.Models;
using Proyecto_Final.ViewModels.Comunes;

namespace Proyecto_Final.ViewModels.Calificaciones;

public sealed class ConfigurarEvaluacion
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una asignación.")]
    public int AsignacionId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un período.")]
    public int PeriodoId { get; set; }

    public MetodoCalculoEvaluacion MetodoCalculo { get; set; } =
        MetodoCalculoEvaluacion.SumaPuntos;

    public int? PlantillaId { get; set; }

    public bool GuardarComoPlantilla { get; set; }

    [StringLength(150, ErrorMessage = "El nombre de la plantilla no puede superar los 150 caracteres.")]
    public string? NombrePlantilla { get; set; }

    public List<CategoriaEvaluacionFormulario> Categorias { get; set; } = [];
    public List<OpcionSeleccion> Asignaciones { get; set; } = [];
    public List<OpcionSeleccion> Periodos { get; set; } = [];
    public List<OpcionSeleccion> Tareas { get; set; } = [];
    public List<OpcionSeleccion> Plantillas { get; set; } = [];
}

public sealed class CategoriaEvaluacionFormulario
{
    [Required(ErrorMessage = "El nombre de la categoría es obligatorio."), StringLength(100, ErrorMessage = "El nombre de la categoría no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    public TipoCategoriaEvaluacion Tipo { get; set; }

    [Range(typeof(decimal), "0.01", "100", ErrorMessage = "Ingrese un porcentaje entre 0.01 y 100.")]
    public decimal Porcentaje { get; set; }

    public List<ActividadEvaluableFormulario> Actividades { get; set; } = [];
}

public sealed class ActividadEvaluableFormulario
{
    [Required(ErrorMessage = "El nombre de la actividad es obligatorio."), StringLength(200, ErrorMessage = "El nombre de la actividad no puede superar los 200 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999", ErrorMessage = "Ingrese un punteo máximo válido.")]
    public decimal PunteoMaximo { get; set; }

    public int? TareaId { get; set; }
}

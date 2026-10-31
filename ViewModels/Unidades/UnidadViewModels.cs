using System.ComponentModel.DataAnnotations;
using Proyecto_Final.Models;
using Proyecto_Final.ViewModels.Comunes;

namespace Proyecto_Final.ViewModels.Unidades;

public class FormularioUnidad
{
    [Required(ErrorMessage = "El título es obligatorio."), StringLength(150, ErrorMessage = "El título no puede superar los 150 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "La descripción no puede superar los 1000 caracteres.")]
    public string? Descripcion { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "El orden debe ser mayor que cero.")]
    public int AsignacionId { get; set; }

    public int? PeriodoId { get; set; }

    public DateTime? FechaDisponibilidad { get; set; }

    public DateTime? FechaCierreAcceso { get; set; }

    public List<OpcionSeleccion> Asignaciones { get; set; } = [];

    public List<OpcionSeleccion> Periodos { get; set; } = [];
}

public sealed class CrearUnidad : FormularioUnidad
{
}

public sealed class EditarUnidad : FormularioUnidad
{
    // Nombre explícito para que la clave de Unidad no compita con el parámetro
    // de ruta de la asignación académica durante el model binding del POST.
    [Range(1, int.MaxValue)]
    public int UnidadId { get; set; }

    [Range(1, int.MaxValue)]
    public int UnidadAsignacionId { get; set; }

    public int Orden { get; set; }
}

public sealed class UnidadLista
{
    public int Id { get; set; }

    public int UnidadAsignacionId { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Curso { get; set; } = string.Empty;

    public string Seccion { get; set; } = string.Empty;

    public int Orden { get; set; }

    public EstadoPublicacionUnidad Estado { get; set; }

    public DateTime? Disponibilidad { get; set; }

    public int Materiales { get; set; }
}

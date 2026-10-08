using System.ComponentModel.DataAnnotations;
using Proyecto_Final.Models;
using Proyecto_Final.ViewModels.Comunes;

namespace Proyecto_Final.ViewModels.Unidades;

public class FormularioUnidad
{
    [Required, StringLength(150)]
    public string Titulo { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Descripcion { get; set; }

    [Range(1, int.MaxValue)]
    public int AsignacionId { get; set; }

    public int? PeriodoId { get; set; }

    [Range(1, int.MaxValue)]
    public int Orden { get; set; } = 1;

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
    public int Id { get; set; }

    public int UnidadAsignacionId { get; set; }
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
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Proyecto_Final.Models;
using Proyecto_Final.ViewModels.Comunes;

namespace Proyecto_Final.ViewModels.Tareas;

public class FormularioTarea
{
    [Range(1, int.MaxValue)]
    public int AsignacionId { get; set; }

    public int? UnidadAsignacionId { get; set; }

    public TipoTarea Tipo { get; set; } =
        TipoTarea.Archivo;

    [Required, StringLength(200)]
    public string Titulo { get; set; } = string.Empty;

    public string? Instrucciones { get; set; }

    [Range(typeof(decimal), "0", "9999999")]
    public decimal PunteoMaximo { get; set; } = 100;

    public DateTime? FechaDisponibilidad { get; set; }

    public DateTime? FechaLimite { get; set; }

    public bool PermitirEntregaTardia { get; set; }

    public IFormFile? Archivo { get; set; }

    public List<OpcionSeleccion> Asignaciones { get; set; } = [];

    public List<OpcionSeleccion> Unidades { get; set; } = [];
}

public sealed class CrearTarea : FormularioTarea
{
}

public sealed class EditarTarea : FormularioTarea
{
    public int Id { get; set; }
}

public sealed class TareaLista
{
    public int Id { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Curso { get; set; } = string.Empty;

    public string Seccion { get; set; } = string.Empty;

    public TipoTarea Tipo { get; set; }

    public EstadoTarea Estado { get; set; }

    public DateTime? Limite { get; set; }

    public int Pendientes { get; set; }

    public int Entregadas { get; set; }
}
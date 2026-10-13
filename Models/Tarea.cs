
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Final.Models;

public class Tarea
{
    public int Id { get; set; }

    public int AsignacionId { get; set; }
    public Asignacion Asignacion { get; set; } = null!;

    public int? UnidadAsignacionId { get; set; }
    public UnidadAsignacion? UnidadAsignacion { get; set; }

    public TipoTarea Tipo { get; set; }

    [Required, StringLength(200)]
    public string Titulo { get; set; } = string.Empty;

    public string? Instrucciones { get; set; }

    public EstadoTarea Estado { get; set; } =
        EstadoTarea.Borrador;

    [Precision(9, 2)]
    public decimal PunteoMaximo { get; set; }

    public DateTime? FechaDisponibilidad { get; set; }
    public DateTime? FechaLimite { get; set; }

    public bool PermitirEntregaTardia { get; set; }

    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaPublicacion { get; set; }
    public DateTime? FechaCierre { get; set; }

    [Required]
    public string CreadoPorUsuarioId { get; set; } =
        string.Empty;

    public Usuario CreadoPorUsuario { get; set; } = null!;

    public Cuestionario? Cuestionario { get; set; }

    public ICollection<TareaArchivo> Archivos { get; set; } =
        new List<TareaArchivo>();

    public ICollection<Entrega> Entregas { get; set; } =
        new List<Entrega>();

    public Guid GrupoVersionId { get; set; }

    [Range(1, int.MaxValue)]
    public int NumeroVersion { get; set; } = 1;

    public int? TareaAnteriorId { get; set; }

    public Tarea? TareaAnterior { get; set; }
}

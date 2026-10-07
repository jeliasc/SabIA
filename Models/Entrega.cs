
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Final.Models;

public class Entrega
{
    public int Id { get; set; }

    public int TareaId { get; set; }
    public Tarea Tarea { get; set; } = null!;

    public int AlumnoId { get; set; }
    public Alumno Alumno { get; set; } = null!;

    [Range(1, int.MaxValue)]
    public int NumeroEnvio { get; set; } = 1;

    public EstadoEntrega Estado { get; set; } = EstadoEntrega.Enviada;

    public string? ComentarioAlumno { get; set; }

    public DateTime FechaEntrega { get; set; }

    [Precision(9, 2)]
    public decimal? Calificacion { get; set; }

    public string? Retroalimentacion { get; set; }

    public DateTime? FechaCalificacion { get; set; }

    public string? CalificadoPorUsuarioId { get; set; }
    public Usuario? CalificadoPorUsuario { get; set; }

    public ICollection<EntregaArchivo> Archivos { get; set; } = new List<EntregaArchivo>();
}

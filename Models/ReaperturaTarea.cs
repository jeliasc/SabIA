using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public enum AlcanceReaperturaTarea
{
    SoloPendientes = 1,
    Todos = 2
}

public sealed class ReaperturaTarea
{
    public int Id { get; set; }
    public int TareaId { get; set; }
    public Tarea Tarea { get; set; } = null!;
    public DateTime FechaReapertura { get; set; }
    public DateTime FechaLimite { get; set; }
    public AlcanceReaperturaTarea Alcance { get; set; }
    public Guid TokenSolicitud { get; set; }

    [Required, StringLength(500)]
    public string Motivo { get; set; } = string.Empty;

    [Required]
    public string ReabiertaPorUsuarioId { get; set; } = string.Empty;
    public Usuario ReabiertaPorUsuario { get; set; } = null!;
    public ICollection<ReaperturaTareaAlumno> Alumnos { get; set; } = new List<ReaperturaTareaAlumno>();
}

// Conserva los alumnos efectivamente beneficiados aunque su inscripción cambie.
public sealed class ReaperturaTareaAlumno
{
    public int ReaperturaTareaId { get; set; }
    public ReaperturaTarea ReaperturaTarea { get; set; } = null!;
    public int AlumnoId { get; set; }
    public Alumno Alumno { get; set; } = null!;
    public int? EntregaId { get; set; }
    public Entrega? Entrega { get; set; }
    public bool OportunidadAdicional { get; set; }
}

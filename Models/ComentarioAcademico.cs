using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

/// <summary>
/// Conversación académica privada, ligada a una actividad y a un alumno.
/// No representa una entrega ni altera su estado o calificación.
/// </summary>
public sealed class ComentarioAcademico
{
    public int Id { get; set; }
    public int TareaId { get; set; }
    public Tarea Tarea { get; set; } = null!;
    public int AlumnoId { get; set; }
    public Alumno Alumno { get; set; } = null!;
    [Required]
    public string AutorUsuarioId { get; set; } = string.Empty;
    public Usuario AutorUsuario { get; set; } = null!;
    [Required, StringLength(4000)]
    public string Contenido { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public ICollection<ComentarioAcademicoArchivo> Archivos { get; set; } =
        new List<ComentarioAcademicoArchivo>();
}

public sealed class ComentarioAcademicoArchivo
{
    public int ComentarioAcademicoId { get; set; }
    public ComentarioAcademico ComentarioAcademico { get; set; } = null!;
    public int ArchivoId { get; set; }
    public Archivo Archivo { get; set; } = null!;
}

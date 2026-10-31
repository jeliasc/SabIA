using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Proyecto_Final.ViewModels.ComentariosAcademicos;

public sealed class ConversacionAcademica
{
    public int TareaId { get; init; }
    public int AlumnoId { get; init; }
    public string Actividad { get; init; } = string.Empty;
    public string Alumno { get; init; } = string.Empty;
    public bool PuedeComentar { get; init; }
    public List<ComentarioAcademicoItem> Comentarios { get; init; } = [];
}

public sealed class ComentarioAcademicoItem
{
    public int Id { get; init; }
    public string Autor { get; init; } = string.Empty;
    public DateTime FechaCreacion { get; init; }
    public string Contenido { get; init; } = string.Empty;
    public List<ComentarioAcademicoArchivoItem> Archivos { get; init; } = [];
}

public sealed class ComentarioAcademicoArchivoItem
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Mime { get; init; } = string.Empty;
    public bool PuedeVisualizar { get; init; }
}

public sealed class CrearComentarioAcademico
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una actividad.")]
    public int TareaId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un alumno.")]
    public int AlumnoId { get; set; }
    [StringLength(4000, ErrorMessage = "El comentario no puede superar los 4000 caracteres.")]
    public string? Contenido { get; set; }
    public IFormFile? Archivo { get; set; }
}

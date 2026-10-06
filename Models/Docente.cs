using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Final.Models;

public class Docente
{
    public int Id { get; set; }

    [Required]
    [Column("Usuario_Id")]
    public string UsuarioId { get; set; } = string.Empty;

    [ForeignKey(nameof(UsuarioId))]
    public Usuario Usuario { get; set; } = null!;

    [Required]
    [StringLength(30)]
    [Column("Carnet")]
    public string Carnet { get; set; } = string.Empty;

    [StringLength(13)]
    [Column("Nit")]
    public string? Nit { get; set; }

    [Column("Nivel_Academico")]
    public NivelAcademico? NivelAcademico { get; set; }

    [StringLength(200)]
    [Column("Titulo")]
    public string? Titulo { get; set; }

    [StringLength(150)]
    [Column("Institucion_Otorgante")]
    public string? InstitucionOtorgante { get; set; }
}
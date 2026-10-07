
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Final.Models;

public class Alumno
{
    public int Id { get; set; }

    [Required]
    [Column("Usuario_Id")]
    public string UsuarioId { get; set; } = string.Empty;

    [ForeignKey(nameof(UsuarioId))]
    public Usuario Usuario { get; set; } = null!;

    [StringLength(20)]
    [Column("Codigo_Personal")]
    public string? CodigoPersonal { get; set; }

    public ICollection<AlumnoEncargado> AlumnoEncargados { get; set; } = new List<AlumnoEncargado>();
}

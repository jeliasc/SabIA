using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public class PlantillaEvaluacion
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    public int? CursoId { get; set; }
    public Curso? Curso { get; set; }

    public MetodoCalculoEvaluacion MetodoCalculo { get; set; } =
        MetodoCalculoEvaluacion.CategoriasPonderadas;

    [Required]
    public string DefinicionJson { get; set; } = string.Empty;

    public bool Activa { get; set; } = true;

    public DateTime FechaCreacion { get; set; }

    [Required]
    public string CreadoPorUsuarioId { get; set; } = string.Empty;
    public Usuario CreadoPorUsuario { get; set; } = null!;
}

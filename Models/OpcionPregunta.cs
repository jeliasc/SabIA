
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public class OpcionPregunta
{
    public int Id { get; set; }

    public int PreguntaId { get; set; }
    public Pregunta Pregunta { get; set; } = null!;

    [Range(1, int.MaxValue)]
    public int Orden { get; set; }

    [Required]
    public string Texto { get; set; } = string.Empty;

    public bool EsCorrecta { get; set; }
}


using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public class RespuestaAceptada
{
    public int Id { get; set; }

    public int PreguntaId { get; set; }
    public Pregunta Pregunta { get; set; } = null!;

    [Required]
    [StringLength(1000)]
    public string Texto { get; set; } = string.Empty;
}

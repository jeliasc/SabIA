
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Final.Models;

public class Pregunta
{
    public int Id { get; set; }

    public int CuestionarioId { get; set; }
    public Cuestionario Cuestionario { get; set; } = null!;

    [Range(1, int.MaxValue)]
    public int Orden { get; set; }

    public TipoPregunta Tipo { get; set; }

    [Required]
    public string Enunciado { get; set; } = string.Empty;

    [Precision(9, 2)]
    public decimal Punteo { get; set; }

    public int? TiempoLimiteSegundos { get; set; }

    public ICollection<OpcionPregunta> Opciones { get; set; } = new List<OpcionPregunta>();

    public ICollection<RespuestaAceptada> RespuestasAceptadas { get; set; } = new List<RespuestaAceptada>();
}

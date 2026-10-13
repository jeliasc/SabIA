using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public class ConfiguracionNotaAnual
{
    public int Id { get; set; }

    public int CicloEscolarId { get; set; }

    public CicloEscolar CicloEscolar { get; set; } = null!;

    public MetodoCalculoAnual MetodoCalculo { get; set; } = MetodoCalculoAnual.PromedioSimple;

    public bool Activa { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaActivacion { get; set; }

    [Required]
    public string ConfiguradoPorUsuarioId { get; set; } = string.Empty;

    public Usuario ConfiguradoPorUsuario { get; set; } = null!;

    public ICollection<PonderacionPeriodoAnual> Ponderaciones { get; set; } = new List<PonderacionPeriodoAnual>();
}
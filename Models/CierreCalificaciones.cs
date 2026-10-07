namespace Proyecto_Final.Models;

public class CierreCalificaciones
{
    public int Id { get; set; }

    public int ConfiguracionEvaluacionId { get; set; }

    public ConfiguracionEvaluacion ConfiguracionEvaluacion { get; set; } = null!;

    public EstadoCierreCalificaciones Estado { get; set; } = EstadoCierreCalificaciones.Abierto;

    public DateTime? FechaCierre { get; set; }

    public string? CerradoPorUsuarioId { get; set; }

    public Usuario? CerradoPorUsuario { get; set; }

    public ICollection<MovimientoCierreCalificaciones> Movimientos { get; set; } = new List<MovimientoCierreCalificaciones>();
}
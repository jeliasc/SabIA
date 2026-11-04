using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public class EventoDiagnostico
{
    public long Id { get; set; }
    public Guid SesionDiagnosticoId { get; set; }
    public SesionDiagnostico SesionDiagnostico { get; set; } = null!;
    public DateTime FechaUtc { get; set; }
    [Required, StringLength(80)] public string Modulo { get; set; } = string.Empty;
    [StringLength(120)] public string? Controlador { get; set; }
    [StringLength(120)] public string? Accion { get; set; }
    public int? ResultadoHttp { get; set; }
    [Required, StringLength(120)] public string Operacion { get; set; } = string.Empty;
    [StringLength(250)] public string? TipoExcepcion { get; set; }
    [StringLength(2000)] public string? MensajeTecnico { get; set; }
    [StringLength(250)] public string? Componente { get; set; }
    [StringLength(64)] public string? CorrelationId { get; set; }
    public long? DuracionMilisegundos { get; set; }
    [StringLength(80)] public string? Categoria { get; set; }
}

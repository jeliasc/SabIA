using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public class RegistroAuditoria
{
    public long Id { get; set; }
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
    [StringLength(450)] public string? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    [Required, StringLength(80)] public string Modulo { get; set; } = string.Empty;
    [Required, StringLength(120)] public string Accion { get; set; } = string.Empty;
    public TipoEventoAuditoria Tipo { get; set; }
    public ResultadoAuditoria Resultado { get; set; }
    [StringLength(120)] public string? Entidad { get; set; }
    [StringLength(128)] public string? EntidadId { get; set; }
    [Required, StringLength(2000)] public string Descripcion { get; set; } = string.Empty;
    [StringLength(8000)] public string? ValoresAnteriores { get; set; }
    [StringLength(8000)] public string? ValoresNuevos { get; set; }
    [StringLength(1000)] public string? Justificacion { get; set; }
    [StringLength(45)] public string? DireccionIp { get; set; }
    [StringLength(512)] public string? UserAgent { get; set; }
    [StringLength(512)] public string? Ruta { get; set; }
    [StringLength(16)] public string? MetodoHttp { get; set; }
    [StringLength(64)] public string? CorrelationId { get; set; }
}

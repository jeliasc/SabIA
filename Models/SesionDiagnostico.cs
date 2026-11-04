using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public class SesionDiagnostico
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public string UsuarioId { get; set; } = string.Empty;
    public Usuario Usuario { get; set; } = null!;
    [StringLength(80)] public string? Modulo { get; set; }
    public int DuracionMinutos { get; set; }
    [Required, StringLength(1000)] public string Motivo { get; set; } = string.Empty;
    [Required] public string ActivadaPorUsuarioId { get; set; } = string.Empty;
    public Usuario ActivadaPorUsuario { get; set; } = null!;
    public DateTime InicioUtc { get; set; }
    public DateTime VencimientoUtc { get; set; }
    public EstadoSesionDiagnostico Estado { get; set; } = EstadoSesionDiagnostico.Activa;
    public DateTime? FinalizadaUtc { get; set; }
    public string? FinalizadaPorUsuarioId { get; set; }
    public Usuario? FinalizadaPorUsuario { get; set; }
    [StringLength(500)] public string? MotivoFinalizacion { get; set; }
    public ICollection<EventoDiagnostico> Eventos { get; set; } = new List<EventoDiagnostico>();
}

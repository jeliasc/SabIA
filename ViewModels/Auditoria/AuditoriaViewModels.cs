using System.ComponentModel.DataAnnotations;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Auditoria;

namespace Proyecto_Final.ViewModels.Auditoria;

public sealed class AuditoriaFiltro
{
    [DataType(DataType.Date)]
    [Display(Name = "Desde (Guatemala)")]
    public DateOnly? FechaDesde { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Hasta (Guatemala)")]
    public DateOnly? FechaHasta { get; set; }

    [StringLength(100)]
    public string? Responsable { get; set; }

    [StringLength(80)]
    [Display(Name = "Módulo")]
    public string? Modulo { get; set; }

    [StringLength(120)]
    [Display(Name = "Acción")]
    public string? Accion { get; set; }

    [StringLength(45)]
    [Display(Name = "Dirección IP")]
    public string? DireccionIp { get; set; }

    public ResultadoAuditoria? Resultado { get; set; }
    public int Pagina { get; set; } = 1;
}

public sealed class AuditoriaIndice
{
    public required AuditoriaFiltro Filtro { get; init; }
    public required IReadOnlyList<AuditoriaFila> Registros { get; init; }
    public int TotalRegistros { get; init; }
    public int Pagina { get; init; }
    public int TotalPaginas { get; init; }
}

public sealed class AuditoriaFila
{
    public long Id { get; init; }
    public DateTime FechaUtc { get; init; }
    public DateTime FechaHoraGuatemala => ZonaHorariaAuditoria.ConvertirUtcAGuatemala(FechaUtc);
    public string Responsable { get; init; } = "Sistema";
    public string Modulo { get; init; } = string.Empty;
    public string Accion { get; init; } = string.Empty;
    public ResultadoAuditoria Resultado { get; init; }
    public string? DireccionIp { get; init; }
}

public sealed class AuditoriaDetalle
{
    public long Id { get; init; }
    public DateTime FechaUtc { get; init; }
    public DateTime FechaHoraGuatemala => ZonaHorariaAuditoria.ConvertirUtcAGuatemala(FechaUtc);
    public string Responsable { get; init; } = "Sistema";
    public string? UsuarioId { get; init; }
    public string Modulo { get; init; } = string.Empty;
    public string Accion { get; init; } = string.Empty;
    public TipoEventoAuditoria Tipo { get; init; }
    public ResultadoAuditoria Resultado { get; init; }
    public string? Entidad { get; init; }
    public string? EntidadId { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public string? ValoresAnteriores { get; init; }
    public string? ValoresNuevos { get; init; }
    public string? Justificacion { get; init; }
    public string? DireccionIp { get; init; }
    public string? UserAgent { get; init; }
    public string? Ruta { get; init; }
    public string? MetodoHttp { get; init; }
    public string? CorrelationId { get; init; }
}

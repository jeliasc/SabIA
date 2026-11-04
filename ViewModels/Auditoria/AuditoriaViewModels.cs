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

    [StringLength(100, ErrorMessage = "El responsable no puede superar los 100 caracteres.")]
    public string? Responsable { get; set; }

    [StringLength(80, ErrorMessage = "El módulo no puede superar los 80 caracteres.")]
    [Display(Name = "Módulo")]
    public string? Modulo { get; set; }

    [StringLength(120, ErrorMessage = "La acción no puede superar los 120 caracteres.")]
    [Display(Name = "Acción")]
    public string? Accion { get; set; }

    [StringLength(45, ErrorMessage = "La dirección IP no puede superar los 45 caracteres.")]
    [Display(Name = "Dirección IP")]
    public string? DireccionIp { get; set; }

    public ResultadoAuditoria? Resultado { get; set; }
    public OrigenHistorialAuditoria Origen { get; set; } = OrigenHistorialAuditoria.Todos;
    public int Pagina { get; set; } = 1;
}

public enum OrigenHistorialAuditoria { Todos, Aplicacion, BaseDatos }

public sealed class AuditoriaIndice
{
    public required AuditoriaFiltro Filtro { get; init; }
    public required IReadOnlyList<AuditoriaFila> Registros { get; init; }
    public int TotalRegistros { get; init; }
    public int Pagina { get; init; }
    public int TotalPaginas { get; init; }
}

public sealed class AuditoriaCentroViewModel
{
    public required AuditoriaIndice Historial { get; init; }
    public required DiagnosticoPanel Diagnostico { get; init; }
    public required SolicitudDiagnostico Solicitud { get; init; }
}

public sealed class SolicitudDiagnostico
{
    [Required(ErrorMessage = "Debe seleccionar un usuario.")]
    public string UsuarioId { get; set; } = string.Empty;
    [StringLength(80)] public string? Modulo { get; set; }
    [Range(5, 30)] public int DuracionMinutos { get; set; } = 15;
    [Required, StringLength(1000)] public string Motivo { get; set; } = string.Empty;
}

public sealed class ResultadoDiagnostico
{
    public bool Exitoso { get; private init; }
    public string Mensaje { get; private init; } = string.Empty;
    public static ResultadoDiagnostico Exito(string mensaje) => new() { Exitoso = true, Mensaje = mensaje };
    public static ResultadoDiagnostico Error(string mensaje) => new() { Exitoso = false, Mensaje = mensaje };
}

public sealed class DiagnosticoSolicitudTecnica
{
    public required string UsuarioId { get; init; }
    public string? Modulo { get; init; }
    public string? Controlador { get; init; }
    public string? Accion { get; init; }
    public int? ResultadoHttp { get; init; }
    public string? Operacion { get; init; }
    public string? TipoExcepcion { get; init; }
    public string? MensajeTecnico { get; init; }
    public string? Componente { get; init; }
    public string? CorrelationId { get; init; }
    public long? DuracionMilisegundos { get; init; }
    public string? Categoria { get; init; }
}

public sealed class DiagnosticoPanel
{
    public IReadOnlyList<DiagnosticoSesionFila> Sesiones { get; private init; } = [];
    public IReadOnlyList<DiagnosticoEventoFila> Eventos { get; private init; } = [];
    public IReadOnlyList<string> Modulos { get; private init; } = [];
    public static DiagnosticoPanel Crear(IEnumerable<SesionDiagnostico> sesiones, IEnumerable<EventoDiagnostico> eventos, IEnumerable<string> modulos) => new()
    {
        Sesiones = sesiones.Select(x => new DiagnosticoSesionFila
        {
            Id = x.Id, UsuarioId = x.UsuarioId, Usuario = x.Usuario.PrimerNombre + " " + x.Usuario.PrimerApellido,
            Modulo = x.Modulo, Motivo = x.Motivo, InicioUtc = x.InicioUtc, VencimientoUtc = x.VencimientoUtc,
            Estado = x.Estado, ActivadaPor = x.ActivadaPorUsuario.PrimerNombre + " " + x.ActivadaPorUsuario.PrimerApellido
        }).ToList(),
        Eventos = eventos.Select(x => new DiagnosticoEventoFila
        {
            Id = x.Id, SesionId = x.SesionDiagnosticoId, Usuario = x.SesionDiagnostico.Usuario.PrimerNombre + " " + x.SesionDiagnostico.Usuario.PrimerApellido,
            FechaUtc = x.FechaUtc, Modulo = x.Modulo, Operacion = x.Operacion, ResultadoHttp = x.ResultadoHttp,
            TipoExcepcion = x.TipoExcepcion, MensajeTecnico = x.MensajeTecnico, DuracionMilisegundos = x.DuracionMilisegundos
        }).ToList(),
        Modulos = modulos.ToList()
    };
}

public sealed class DiagnosticoSesionFila
{
    public Guid Id { get; init; } public string UsuarioId { get; init; } = string.Empty; public string Usuario { get; init; } = string.Empty;
    public string? Modulo { get; init; } public string Motivo { get; init; } = string.Empty; public DateTime InicioUtc { get; init; }
    public DateTime VencimientoUtc { get; init; } public EstadoSesionDiagnostico Estado { get; init; } public string ActivadaPor { get; init; } = string.Empty;
}

public sealed class DiagnosticoEventoFila
{
    public long Id { get; init; } public Guid SesionId { get; init; } public string Usuario { get; init; } = string.Empty;
    public DateTime FechaUtc { get; init; } public string Modulo { get; init; } = string.Empty; public string Operacion { get; init; } = string.Empty;
    public int? ResultadoHttp { get; init; } public string? TipoExcepcion { get; init; } public string? MensajeTecnico { get; init; }
    public long? DuracionMilisegundos { get; init; }
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
    public string Origen { get; init; } = "Aplicación";
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

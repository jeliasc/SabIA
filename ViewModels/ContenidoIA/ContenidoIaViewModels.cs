using System.ComponentModel.DataAnnotations;
using Proyecto_Final.Servicios.InteligenciaArtificial;
using Proyecto_Final.ViewModels.Comunes;

namespace Proyecto_Final.ViewModels.ContenidoIA;

public sealed class GenerarContenidoIa
{
    public OrigenDocumentoIa OrigenDocumento { get; set; } = OrigenDocumentoIa.Planificacion;
    public int? PlanificacionId { get; set; }
    public string? Planificacion { get; set; }
    public bool Cuestionario { get; set; }
    [StringLength(2000)]
    public string? InstruccionesCuestionario { get; set; }
    public bool Resumen { get; set; } = true;
    [StringLength(2000)]
    public string? InstruccionesResumen { get; set; }
    public bool HojaTrabajo { get; set; }
    [StringLength(2000)]
    public string? InstruccionesHojaTrabajo { get; set; }
    public bool Glosario { get; set; }
    [StringLength(2000)]
    public string? InstruccionesGlosario { get; set; }
    public IFormFile? Documento { get; set; }
    public List<OpcionSeleccion> Planificaciones { get; set; } = [];
}

public enum OrigenDocumentoIa
{
    Planificacion = 1,
    ArchivoLocal = 2
}

public sealed class GenerarContenidoDirectoIa
{
    [Required]
    [StringLength(8000)]
    public string Prompt { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int MaxTokens { get; set; } = 1000;
}

public sealed class TrabajosContenidoIa
{
    public string Planificacion { get; set; } = string.Empty;
    public List<TrabajoIa> Trabajos { get; set; } = [];
}

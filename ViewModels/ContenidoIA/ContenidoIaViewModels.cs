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
    [StringLength(2000, ErrorMessage = "Las instrucciones del cuestionario no pueden superar los 2000 caracteres.")]
    public string? InstruccionesCuestionario { get; set; }
    public bool Resumen { get; set; } = true;
    [StringLength(2000, ErrorMessage = "Las instrucciones del resumen no pueden superar los 2000 caracteres.")]
    public string? InstruccionesResumen { get; set; }
    public bool HojaTrabajo { get; set; }
    [StringLength(2000, ErrorMessage = "Las instrucciones de la hoja de trabajo no pueden superar los 2000 caracteres.")]
    public string? InstruccionesHojaTrabajo { get; set; }
    public bool Glosario { get; set; }
    [StringLength(2000, ErrorMessage = "Las instrucciones del glosario no pueden superar los 2000 caracteres.")]
    public string? InstruccionesGlosario { get; set; }
    public IFormFile? Documento { get; set; }
    public List<OpcionSeleccion> Planificaciones { get; set; } = [];
}

public enum OrigenDocumentoIa
{
    [Display(Name = "Planificación")]
    Planificacion = 1,
    [Display(Name = "Archivo local")]
    ArchivoLocal = 2
}

public sealed class GenerarContenidoDirectoIa
{
    [Required(ErrorMessage = "La instrucción es obligatoria.")]
    [StringLength(8000, ErrorMessage = "La instrucción no puede superar los 8000 caracteres.")]
    public string Prompt { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Ingrese una cantidad de tokens válida.")]
    public int MaxTokens { get; set; } = 1000;
}

public sealed class TrabajosContenidoIa
{
    public string Planificacion { get; set; } = string.Empty;
    public List<TrabajoIa> Trabajos { get; set; } = [];
}

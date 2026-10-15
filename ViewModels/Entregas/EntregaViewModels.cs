using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Proyecto_Final.Models;

namespace Proyecto_Final.ViewModels.Entregas;

public sealed class EntregaLista
{
    public int Id { get; set; }

    public string Alumno { get; set; } = string.Empty;

    public string Tarea { get; set; } = string.Empty;

    public string Curso { get; set; } = string.Empty;

    public DateTime? FechaLimite { get; set; }

    public DateTime? FechaEntrega { get; set; }

    public EstadoEntrega Estado { get; set; }

    public decimal? Calificacion { get; set; }

    public decimal PunteoMaximo { get; set; }

    public int NumeroEnvio { get; set; }

    public DateTime? FechaLimiteIndividual { get; set; }

    public bool PuedeReabrirse { get; set; }

    public bool PuedeCalificarse { get; set; }

    public bool PuedeEntregarse { get; set; }

    public bool TieneIntentoPendienteEvaluacion { get; set; }

    // Si la tarea es un cuestionario: el alumno lo resuelve en lugar de
    // subir un archivo y el docente puede ver sus respuestas.
    public int? CuestionarioId { get; set; }
}

public sealed class EntregarTarea
{
    public int Id { get; set; }

    public string Tarea { get; set; } = string.Empty;

    public string? Instrucciones { get; set; }

    public DateTime? FechaLimite { get; set; }

    [StringLength(1000)]
    public string? Comentario { get; set; }

    public IFormFile? Archivo { get; set; }

    public List<ArchivoTareaApoyo> ArchivosApoyo { get; set; } = [];
}

public sealed class ArchivoTareaApoyo
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;
}

public sealed class DetalleEntrega
{
    public int EntregaSeleccionadaId { get; set; }
    public string Alumno { get; set; } = string.Empty;
    public string Tarea { get; set; } = string.Empty;
    public decimal PunteoMaximo { get; set; }
    public decimal? CalificacionEfectiva { get; set; }
    public bool TieneIntentoPendienteEvaluacion { get; set; }
    public List<IntentoEntregaDetalle> Intentos { get; set; } = [];
}

public sealed class IntentoEntregaDetalle
{
    public int Id { get; set; }
    public int NumeroEnvio { get; set; }
    public EstadoEntrega Estado { get; set; }
    public DateTime? FechaEntrega { get; set; }
    public decimal? Calificacion { get; set; }
    public string? ComentarioAlumno { get; set; }
    public string? Retroalimentacion { get; set; }
    public string? MotivoReapertura { get; set; }
    public List<ArchivoEntregaDetalle> Archivos { get; set; } = [];
}

public sealed class ArchivoEntregaDetalle
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Mime { get; set; } = string.Empty;
    public bool PuedeVisualizar { get; set; }
}

public sealed class CalificarEntrega
{
    public int Id { get; set; }

    public string Alumno { get; set; } = string.Empty;

    public string Tarea { get; set; } = string.Empty;

    public decimal PunteoMaximo { get; set; }

    public string? ComentarioAlumno { get; set; }

    public DateTime? FechaEntrega { get; set; }

    [Range(typeof(decimal), "0", "9999999")]
    public decimal Calificacion { get; set; }

    [StringLength(4000)]
    public string? Retroalimentacion { get; set; }

    public bool Devolver { get; set; }

    public List<IntentoSeleccionCalificar> Intentos { get; set; } = [];

    public int TareaId { get; set; }
    public int AlumnoId { get; set; }
    public int? InscripcionId { get; set; }
}

public sealed class IntentoSeleccionCalificar
{
    public int Id { get; set; }
    public int NumeroEnvio { get; set; }
    public DateTime? FechaEntrega { get; set; }
    public EstadoEntrega Estado { get; set; }
}

public sealed class ReabrirEntrega
{
    public int Id { get; set; }

    public string Alumno { get; set; } = string.Empty;

    public string Tarea { get; set; } = string.Empty;

    public string Curso { get; set; } = string.Empty;

    public int NumeroEnvioActual { get; set; }

    public EstadoEntrega EstadoActual { get; set; }

    [Required]
    public DateTime FechaLimite { get; set; }

    [Required, StringLength(500, MinimumLength = 5)]
    public string Motivo { get; set; } = string.Empty;
}

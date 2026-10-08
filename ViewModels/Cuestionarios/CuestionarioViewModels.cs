using System.ComponentModel.DataAnnotations;
using Proyecto_Final.Models;

namespace Proyecto_Final.ViewModels.Cuestionarios;

public sealed class CuestionarioLista
{
    public int Id { get; set; }

    public int TareaId { get; set; }

    public string Tarea { get; set; } = string.Empty;

    public string Curso { get; set; } = string.Empty;

    public int Preguntas { get; set; }

    public int MaximoIntentos { get; set; }

    public OrigenCuestionario Origen { get; set; }

    public EstadoTarea EstadoTarea { get; set; }
}

public sealed class ConfigurarCuestionario
{
    public int TareaId { get; set; }

    public string Tarea { get; set; } = string.Empty;

    public ModalidadTiempoCuestionario ModalidadTiempo { get; set; } =
        ModalidadTiempoCuestionario.SinLimite;

    [Range(0, 100)]
    public int MaximoIntentos { get; set; } = 1;

    public CriterioCalificacionIntentos CriterioCalificacion { get; set; } =
        CriterioCalificacionIntentos.MejorNota;

    public bool MostrarResultadosAlFinalizar { get; set; } = true;

    public decimal CalificacionAplicada { get; set; }

    public bool MostrarResultados { get; set; } = true;

    public bool MostrarRespuestasCorrectas { get; set; }
}

public sealed class CrearPregunta
{
    public int CuestionarioId { get; set; }

    public string Cuestionario { get; set; } = string.Empty;

    [Range(1, 500)]
    public int Orden { get; set; } = 1;

    public TipoPregunta Tipo { get; set; } =
        TipoPregunta.SeleccionUnica;

    [Required]
    public string Enunciado { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999")]
    public decimal Punteo { get; set; } = 1;

    [Required]
    public string RespuestaCorrecta { get; set; } = string.Empty;

    public string? OpcionesSeparadas { get; set; }
}

public sealed class ResolverCuestionario
{
    public int CuestionarioId { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public int IntentosUsados { get; set; }

    public int MaximoIntentos { get; set; }

    public List<RespuestaCuestionario> Respuestas { get; set; } = [];
}

public sealed class RespuestaCuestionario
{
    public int PreguntaId { get; set; }

    public int Orden { get; set; }

    public TipoPregunta Tipo { get; set; }

    public string Enunciado { get; set; } = string.Empty;

    public decimal Punteo { get; set; }

    public string? Texto { get; set; }

    public int? OpcionId { get; set; }

    public List<int> OpcionIds { get; set; } = [];

    public List<OpcionCuestionario> Opciones { get; set; } = [];
}

public sealed class OpcionCuestionario
{
    public int Id { get; set; }

    public string Texto { get; set; } = string.Empty;
}

public sealed class ResultadoIntento
{
    public decimal Calificacion { get; set; }

    public decimal PunteoObtenido { get; set; }

    public decimal PunteoTotal { get; set; }

    public decimal CalificacionAplicada { get; set; }

    public bool MostrarResultados { get; set; } = true;

    public bool MostrarRespuestasCorrectas { get; set; }
}
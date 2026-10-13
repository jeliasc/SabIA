
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public class Cuestionario
{
    public int Id { get; set; }

    public int TareaId { get; set; }
    public Tarea Tarea { get; set; } = null!;

    public OrigenCuestionario Origen { get; set; } = OrigenCuestionario.Manual;

    public ModalidadTiempoCuestionario ModalidadTiempo { get; set; } = ModalidadTiempoCuestionario.SinLimite;

    // Tiempo total del cuestionario cuando la modalidad es TiempoGeneral.
    public int? TiempoGeneralSegundos { get; set; }

    // Tiempo predeterminado de cada pregunta cuando la modalidad es TiempoPorPregunta.
    // Cada pregunta puede definir su propio tiempo en Pregunta.TiempoLimiteSegundos.
    public int? TiempoPorPreguntaSegundos { get; set; }

    // Permite al alumno avanzar y regresar entre preguntas.
    // No aplica cuando el tiempo es por pregunta.
    public bool PermitirNavegacion { get; set; }

    // Cada intento presenta las preguntas en un orden distinto.
    public bool OrdenAleatorio { get; set; }

    [Range(0, int.MaxValue)]
    public int MaximoIntentos { get; set; } = 1;

    public CriterioCalificacionIntentos CriterioCalificacion { get; set; } = CriterioCalificacionIntentos.MejorNota;

    public bool MostrarResultadosAlFinalizar { get; set; } = true;
    public bool MostrarRespuestasCorrectas { get; set; }

    public ICollection<Pregunta> Preguntas { get; set; } = new List<Pregunta>();
    public ICollection<IntentoCuestionario> Intentos { get; set; } = new List<IntentoCuestionario>();
}


using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public class Cuestionario
{
    public int Id { get; set; }

    public int TareaId { get; set; }
    public Tarea Tarea { get; set; } = null!;

    public OrigenCuestionario Origen { get; set; } = OrigenCuestionario.Manual;

    public ModalidadTiempoCuestionario ModalidadTiempo { get; set; } = ModalidadTiempoCuestionario.SinLimite;

    public int? TiempoGeneralSegundos { get; set; }

    [Range(0, int.MaxValue)]
    public int MaximoIntentos { get; set; } = 1;

    public CriterioCalificacionIntentos CriterioCalificacion { get; set; } = CriterioCalificacionIntentos.MejorNota;

    public bool MostrarResultadosAlFinalizar { get; set; } = true;

    public bool MostrarRespuestasCorrectas { get; set; }

    public ICollection<Pregunta> Preguntas { get; set; } = new List<Pregunta>();

    public ICollection<IntentoCuestionario> Intentos { get; set; } = new List<IntentoCuestionario>();
}

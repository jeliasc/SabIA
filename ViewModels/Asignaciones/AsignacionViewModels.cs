using System.ComponentModel.DataAnnotations;
using Proyecto_Final.Models;
using Proyecto_Final.ViewModels.Comunes;

namespace Proyecto_Final.ViewModels.Asignaciones;

public class FormularioAsignacion
{
    [Range(1, int.MaxValue)]
    public int SeccionId { get; set; }

    [Range(1, int.MaxValue)]
    public int CursoId { get; set; }

    [Range(1, int.MaxValue)]
    public int DocenteId { get; set; }

    public List<OpcionAsignacion> Secciones { get; set; } = [];

    public List<OpcionAsignacion> Cursos { get; set; } = [];

    public List<OpcionSeleccion> Docentes { get; set; } = [];
}

public sealed class OpcionAsignacion
{
    public int Id { get; set; }

    public int GradoId { get; set; }

    public string Texto { get; set; } = string.Empty;
}

public sealed class CrearAsignacion : FormularioAsignacion
{
}

public sealed class EditarAsignacion : FormularioAsignacion
{
    public int Id { get; set; }
}

public sealed class AsignacionLista
{
    public int Id { get; set; }

    public string Ciclo { get; set; } = string.Empty;

    public string Grado { get; set; } = string.Empty;

    public string Carrera { get; set; } = string.Empty;

    public string Seccion { get; set; } = string.Empty;

    public string Curso { get; set; } = string.Empty;

    public string Docente { get; set; } = string.Empty;

    public EstadoRegistro Estado { get; set; }

    public DateTime? FechaInicioVigencia { get; set; }

    public DateTime? FechaFinVigencia { get; set; }

    public int? AsignacionAnteriorId { get; set; }
}

public sealed class ObligacionesRelevoAsignacion
{
    public int TareasAbiertas { get; set; }
    public int EntregasPendientesCalificacion { get; set; }
    public int CuestionariosEnCurso { get; set; }
    public int CorreccionesPendientes { get; set; }
    public int EvaluacionesSinCerrar { get; set; }
    public int PlanificacionesPendientes { get; set; }

    public bool TienePendientes =>
        TareasAbiertas + EntregasPendientesCalificacion + CuestionariosEnCurso +
        CorreccionesPendientes + EvaluacionesSinCerrar + PlanificacionesPendientes > 0;
}

public sealed class ReemplazarDocenteAsignacion
{
    public int AsignacionId { get; set; }

    public string ContextoAcademico { get; set; } = string.Empty;

    public string DocenteAnterior { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el docente sustituto.")]
    public int DocenteSustitutoId { get; set; }

    [Required, StringLength(1000, MinimumLength = 10)]
    public string Motivo { get; set; } = string.Empty;

    public TipoRelevoAsignacion Tipo { get; set; } = TipoRelevoAsignacion.Ordinario;

    [Required]
    public DateTime FechaEfectiva { get; set; }

    public bool ConfirmarConsecuencias { get; set; }

    public OperacionDelegacionAcademica OperacionesDelegadas { get; set; }

    public ObligacionesRelevoAsignacion Obligaciones { get; set; } = new();

    public List<OpcionSeleccion> Docentes { get; set; } = [];
}

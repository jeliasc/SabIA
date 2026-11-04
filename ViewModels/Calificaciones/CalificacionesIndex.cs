using Proyecto_Final.Models;
using Proyecto_Final.ViewModels.Comunes;

namespace Proyecto_Final.ViewModels.Calificaciones;

public sealed class CalificacionesIndex
{
    public List<CalificacionConfiguracionLista> Configuraciones { get; set; } = [];
    public List<OpcionSeleccion> Asignaciones { get; set; } = [];
    public List<OpcionSeleccion> Periodos { get; set; } = [];
    public bool PuedeConfigurar { get; set; }
}

public sealed class CalificacionConfiguracionLista
{
    public int Id { get; set; }
    public int AsignacionId { get; set; }
    public int PeriodoId { get; set; }
    public string Curso { get; set; } = string.Empty;
    public string GradoSeccion { get; set; } = string.Empty;
    public string Periodo { get; set; } = string.Empty;
    public string Docente { get; set; } = string.Empty;
    public EstadoConfiguracionEvaluacion Estado { get; set; }
    public EstadoCierreCalificaciones EstadoCierre { get; set; }
    public int Actividades { get; set; }
}

/// <summary>
/// Punto de entrada del libro desde un curso. No contiene datos que no hayan
/// sido filtrados previamente por la asignación académica autorizada.
/// </summary>
public sealed class CalificacionesCursoContextual
{
    public int AsignacionId { get; set; }
    public string Curso { get; set; } = string.Empty;
    public string GradoSeccion { get; set; } = string.Empty;
    public string Ciclo { get; set; } = string.Empty;
    public bool PuedeConfigurar { get; set; }
    public List<PeriodoLibroContextual> Periodos { get; set; } = [];
}

public sealed class PeriodoLibroContextual
{
    public int PeriodoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int? ConfiguracionId { get; set; }
    public EstadoCierreCalificaciones EstadoCierre { get; set; } =
        EstadoCierreCalificaciones.Abierto;
    public int Actividades { get; set; }
}

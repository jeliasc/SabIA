using Proyecto_Final.ViewModels.Comunes;

namespace Proyecto_Final.ViewModels.Calificaciones;

/// <summary>Consulta independiente: no participa en el cálculo oficial.</summary>
public sealed class ControlTareas
{
    public int AsignacionId { get; set; }
    public string Curso { get; set; } = string.Empty;
    public string GradoSeccion { get; set; } = string.Empty;
    public List<OpcionSeleccion> Asignaciones { get; set; } = [];
    public List<ControlTareaColumna> Tareas { get; set; } = [];
    public List<ControlTareaFila> Alumnos { get; set; } = [];
}

public sealed class ControlTareaColumna
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public decimal PunteoMaximo { get; set; }
}

public sealed class ControlTareaFila
{
    public string Alumno { get; set; } = string.Empty;
    public string CodigoPersonal { get; set; } = string.Empty;
    public bool EsHistorico { get; set; }
    public Dictionary<int, decimal?> Notas { get; set; } = [];
}

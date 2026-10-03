namespace Proyecto_Final.ViewModels.Prototipo;

public class DashboardPrototipo
{
    public List<IndicadorPrototipo> Indicadores { get; set; } = [];
    public List<ActividadPrototipo> ActividadReciente { get; set; } = [];
    public List<AlertaPrototipo> Alertas { get; set; } = [];
    public List<CursoResumenPrototipo> Cursos { get; set; } = [];
}

public class ActividadPrototipo
{
    public string Icono { get; set; } = "bi bi-circle";
    public string Titulo { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public string Hace { get; set; } = string.Empty;
}

public class AlertaPrototipo
{
    public string Tipo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public string Clase { get; set; } = "warning";
}

public class CursoResumenPrototipo
{
    public string Curso { get; set; } = string.Empty;
    public string GradoSeccion { get; set; } = string.Empty;
    public string Docente { get; set; } = string.Empty;
    public int Progreso { get; set; }
    public int EntregasPendientes { get; set; }
}

public class ExperienciaDocentePrototipo
{
    public string Docente { get; set; } = string.Empty;
    public List<IndicadorPrototipo> Indicadores { get; set; } = [];
    public List<CursoResumenPrototipo> Cursos { get; set; } = [];
    public List<EntregaPendientePrototipo> Entregas { get; set; } = [];
}

public class EntregaPendientePrototipo
{
    public string Alumno { get; set; } = string.Empty;
    public string Tarea { get; set; } = string.Empty;
    public string Curso { get; set; } = string.Empty;
    public string Fecha { get; set; } = string.Empty;
}

public class ExperienciaAlumnoPrototipo
{
    public string Alumno { get; set; } = string.Empty;
    public string GradoSeccion { get; set; } = string.Empty;
    public int ProgresoGeneral { get; set; }
    public List<IndicadorPrototipo> Indicadores { get; set; } = [];
    public List<UnidadAlumnoPrototipo> Unidades { get; set; } = [];
    public List<TareaAlumnoPrototipo> Tareas { get; set; } = [];
}

public class UnidadAlumnoPrototipo
{
    public string Curso { get; set; } = string.Empty;
    public string Unidad { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int Progreso { get; set; }
}

public class TareaAlumnoPrototipo
{
    public string Tarea { get; set; } = string.Empty;
    public string Curso { get; set; } = string.Empty;
    public string FechaLimite { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}

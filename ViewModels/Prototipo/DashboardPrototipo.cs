namespace Proyecto_Final.ViewModels.Prototipo;

public class DashboardPrototipo
{
    public string Tipo { get; set; } = "inicio";
    public List<ContextoDashboard> Contextos { get; set; } = [];
    public List<AccesoDashboard> Accesos { get; set; } = [];
    public List<IndicadorPrototipo> Indicadores { get; set; } = [];
    public List<ActividadPrototipo> ActividadReciente { get; set; } = [];
    public List<AlertaPrototipo> Alertas { get; set; } = [];
    public List<CursoResumenPrototipo> Cursos { get; set; } = [];
    public List<PlanificacionResumenDashboard> Planificaciones { get; set; } = [];
    public List<EntregaPendientePrototipo> Entregas { get; set; } = [];
    public List<ReaperturaResumenDashboard> Reaperturas { get; set; } = [];
}

public sealed class ResultadoDashboard
{
    public bool Permitido { get; set; } = true;
    public string Titulo { get; set; } = "Inicio";
    public string Descripcion { get; set; } = "Bienvenido a SabIA.";
    public DashboardPrototipo Modelo { get; set; } = new();
}

public sealed class ContextoDashboard
{
    public string Codigo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Icono { get; set; } = "bi bi-grid";
}

public sealed class AccesoDashboard
{
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Controlador { get; set; } = string.Empty;
    public string Accion { get; set; } = "Index";
    public string Icono { get; set; } = "bi bi-arrow-right-circle";
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

public class EntregaPendientePrototipo
{
    public int Id { get; set; }
    public string Alumno { get; set; } = string.Empty;
    public string Tarea { get; set; } = string.Empty;
    public string Curso { get; set; } = string.Empty;
    public string Fecha { get; set; } = string.Empty;
}

public sealed class PlanificacionResumenDashboard
{
    public string Titulo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Periodo { get; set; } = string.Empty;
}

public sealed class ReaperturaResumenDashboard
{
    public string Alumno { get; set; } = string.Empty;
    public string Tarea { get; set; } = string.Empty;
    public DateTime FechaLimite { get; set; }
}

public class ExperienciaDocentePrototipo
{
    public string Docente { get; set; } = string.Empty;
    public List<IndicadorPrototipo> Indicadores { get; set; } = [];
    public List<CursoResumenPrototipo> Cursos { get; set; } = [];
    public List<EntregaPendientePrototipo> Entregas { get; set; } = [];
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

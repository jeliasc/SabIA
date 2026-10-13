using Proyecto_Final.Models;

namespace Proyecto_Final.ViewModels.Calificaciones;

public sealed class LibroCalificaciones
{
    public int ConfiguracionId { get; set; }
    public string Curso { get; set; } = string.Empty;
    public string GradoSeccion { get; set; } = string.Empty;
    public string Periodo { get; set; } = string.Empty;
    public string Ciclo { get; set; } = string.Empty;
    public MetodoCalculoEvaluacion MetodoCalculo { get; set; }
    public EstadoCierreCalificaciones EstadoCierre { get; set; }
    public bool PuedeEditar { get; set; }
    public bool PuedeCerrar { get; set; }
    public List<ColumnaActividadCalificacion> Actividades { get; set; } = [];
    public List<FilaAlumnoCalificacion> Alumnos { get; set; } = [];
}

public sealed class ColumnaActividadCalificacion
{
    public int Id { get; set; }
    public int CategoriaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public TipoCategoriaEvaluacion TipoCategoria { get; set; }
    public decimal PorcentajeCategoria { get; set; }
    public decimal MaximoCategoria { get; set; }
    public decimal PunteoMaximo { get; set; }
    public bool EsAutomatica { get; set; }
}

public sealed class FilaAlumnoCalificacion
{
    public int AlumnoId { get; set; }
    public int InscripcionId { get; set; }
    public string CodigoPersonal { get; set; } = string.Empty;
    public string Alumno { get; set; } = string.Empty;
    public List<CeldaCalificacion> Calificaciones { get; set; } = [];
    public decimal Desempeno { get; set; }
    public decimal Actitudinal { get; set; }
    public decimal NotaBimestral { get; set; }
    public decimal AbacusDesempeno { get; set; }
    public decimal AbacusActitudinal { get; set; }
    public int Pendientes { get; set; }
}

public sealed class CeldaCalificacion
{
    public int ActividadId { get; set; }
    public decimal? Nota { get; set; }
    public bool EsAutomatica { get; set; }
}

public sealed class GuardarLibroCalificaciones
{
    public int ConfiguracionId { get; set; }
    public List<NotaLibroCalificacion> Notas { get; set; } = [];
}

public sealed class NotaLibroCalificacion
{
    public int ActividadId { get; set; }
    public int InscripcionId { get; set; }
    public decimal? Nota { get; set; }
}

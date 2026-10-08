using Proyecto_Final.ViewModels.Comunes;

namespace Proyecto_Final.ViewModels.Calificaciones;

public sealed class ResumenAbacus
{
    public int ConfiguracionId { get; set; }
    public string Curso { get; set; } = string.Empty;
    public string GradoSeccion { get; set; } = string.Empty;
    public string CicloPeriodo { get; set; } = string.Empty;
    public bool EsResultadoCerrado { get; set; }
    public bool PuedeSolicitarCorreccion { get; set; }
    public List<OpcionSeleccion> Actividades { get; set; } = [];
    public List<FilaResumenAbacus> Alumnos { get; set; } = [];
}

public sealed class FilaResumenAbacus
{
    public int ResultadoId { get; set; }
    public int InscripcionId { get; set; }
    public string CodigoPersonal { get; set; } = string.Empty;
    public string Alumno { get; set; } = string.Empty;
    public decimal Desempeno { get; set; }
    public decimal Actitudinal { get; set; }
    public decimal NotaBimestral { get; set; }
    public decimal AbacusDesempeno { get; set; }
    public decimal AbacusActitudinal { get; set; }
}

using Microsoft.EntityFrameworkCore;

namespace Proyecto_Final.Models;

public class ResultadoCalificacionPeriodo
{
    public int Id { get; set; }

    public int ConfiguracionEvaluacionId { get; set; }
    public ConfiguracionEvaluacion ConfiguracionEvaluacion { get; set; } = null!;

    public int InscripcionId { get; set; }
    public Inscripcion Inscripcion { get; set; } = null!;

    public int AlumnoId { get; set; }
    public Alumno Alumno { get; set; } = null!;

    [Precision(9, 4)]
    public decimal Desempeno { get; set; }

    [Precision(9, 4)]
    public decimal Actitudinal { get; set; }

    [Precision(9, 4)]
    public decimal NotaBimestral { get; set; }

    [Precision(9, 4)]
    public decimal AbacusDesempeno { get; set; }

    [Precision(9, 4)]
    public decimal AbacusActitudinal { get; set; }

    public DateTime FechaCierre { get; set; }
}

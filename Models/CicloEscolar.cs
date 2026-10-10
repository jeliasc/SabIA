
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public class CicloEscolar
{
    public int Id { get; set; }

    [Range(2000, 9999)]
    public int Anio { get; set; }

    public DateOnly FechaInicio { get; set; }

    public DateOnly FechaFin { get; set; }

    public bool Activo { get; set; } = false;

    public EstadoCicloEscolar Estado { get; set; } = EstadoCicloEscolar.Preparacion;

    public ICollection<Periodo> Periodos { get; set; } = new List<Periodo>();

    public ICollection<Seccion> Secciones { get; set; } = new List<Seccion>();

    public ICollection<MovimientoCicloEscolar> Movimientos { get; set; } = new List<MovimientoCicloEscolar>();
}

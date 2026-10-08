using System.ComponentModel.DataAnnotations;
using Proyecto_Final.Servicios.InteligenciaArtificial;
using Proyecto_Final.ViewModels.Comunes;

namespace Proyecto_Final.ViewModels.ContenidoIA;

public sealed class GenerarContenidoIa
{
    [Range(1, int.MaxValue)]
    public int PlanificacionId { get; set; }
    public string? Planificacion { get; set; }
    public bool Cuestionario { get; set; }
    public bool Resumen { get; set; } = true;
    public bool HojaTrabajo { get; set; }
    public bool Glosario { get; set; }
    public List<OpcionSeleccion> Planificaciones { get; set; } = [];
}

public sealed class TrabajosContenidoIa
{
    public string Planificacion { get; set; } = string.Empty;
    public List<TrabajoIa> Trabajos { get; set; } = [];
}

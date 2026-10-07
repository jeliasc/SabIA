
namespace Proyecto_Final.Models;

public class PlanificacionArchivo
{
    public int PlanificacionId { get; set; }

    public Planificacion Planificacion { get; set; } = null!;

    public int ArchivoId { get; set; }

    public Archivo Archivo { get; set; } = null!;

    public TipoArchivoPlanificacion Tipo { get; set; } = TipoArchivoPlanificacion.Adjunto;
}

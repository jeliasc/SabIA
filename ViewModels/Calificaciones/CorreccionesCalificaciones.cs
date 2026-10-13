using System.ComponentModel.DataAnnotations;
using Proyecto_Final.Models;

namespace Proyecto_Final.ViewModels.Calificaciones;

public sealed class CorreccionesCalificaciones
{
    public List<CorreccionCalificacionLista> Solicitudes { get; set; } = [];
    public bool PuedeRevisar { get; set; }
}

public sealed class CorreccionCalificacionLista
{
    public int Id { get; set; }
    public string Curso { get; set; } = string.Empty;
    public string Periodo { get; set; } = string.Empty;
    public string Alumno { get; set; } = string.Empty;
    public string Actividad { get; set; } = string.Empty;
    public decimal NotaAnterior { get; set; }
    public decimal NotaPropuesta { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public EstadoSolicitudCorreccion Estado { get; set; }
    public DateTime FechaSolicitud { get; set; }
    public Guid VersionConcurrencia { get; set; }
    public bool PuedeAplicar { get; set; }
}

public sealed class SolicitarCorreccionCalificacion
{
    [Range(1, int.MaxValue)]
    public int ConfiguracionId { get; set; }

    [Range(1, int.MaxValue)]
    public int ResultadoId { get; set; }

    [Range(1, int.MaxValue)]
    public int ActividadId { get; set; }

    [Range(typeof(decimal), "0", "9999999")]
    public decimal NotaPropuesta { get; set; }

    [Required, StringLength(1000, MinimumLength = 10)]
    public string Motivo { get; set; } = string.Empty;
}

public sealed class RevisarCorreccionCalificacion
{
    public int Id { get; set; }
    public bool Aprobar { get; set; }
    public Guid VersionConcurrencia { get; set; }

    [StringLength(1000)]
    public string? Observacion { get; set; }
}

public sealed class AplicarCorreccionCalificacion
{
    public int Id { get; set; }
    public Guid VersionConcurrencia { get; set; }
}

using System.ComponentModel.DataAnnotations;
using Proyecto_Final.Models;
using Proyecto_Final.ViewModels.Comunes;

namespace Proyecto_Final.ViewModels.Inscripciones;

public class FormularioInscripcion
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un alumno.")]
    public int AlumnoId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una sección.")]
    public int SeccionId { get; set; }

    public DateOnly Fecha { get; set; } =
        DateOnly.FromDateTime(DateTime.Today);

    public List<OpcionSeleccion> Alumnos { get; set; } = [];

    public List<OpcionSeleccion> Secciones { get; set; } = [];
}

public sealed class CrearInscripcion : FormularioInscripcion
{
}

public sealed class InscripcionLista
{
    public int Id { get; set; }

    public string Alumno { get; set; } = string.Empty;

    public string Seccion { get; set; } = string.Empty;

    public string Carrera { get; set; } = string.Empty;

    public int Ciclo { get; set; }

    public DateOnly Fecha { get; set; }

    public EstadoInscripcion Estado { get; set; }
}

public class TrasladarInscripcion
{
    public int Id { get; set; }

    public string Alumno { get; set; } = string.Empty;

    public int SeccionOrigenId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una sección de destino.")]
    public int SeccionDestinoId { get; set; }

    [Display(Name = "Fecha efectiva académica")]
    public DateOnly? FechaEfectiva { get; set; }

    [Required(ErrorMessage = "El motivo es obligatorio."), StringLength(500, ErrorMessage = "El motivo no puede superar los 500 caracteres.")]
    public string Motivo { get; set; } = string.Empty;

    public List<OpcionSeleccion> Secciones { get; set; } = [];
}

public sealed class RevisionTrasladoInscripcion : TrasladarInscripcion
{
    public string CicloEscolar { get; set; } = string.Empty;
    public string SeccionOrigen { get; set; } = string.Empty;
    public string SeccionDestino { get; set; } = string.Empty;
    public string HuellaImpacto { get; set; } = string.Empty;
    public string TokenRevision { get; set; } = string.Empty;
    public bool TienePeriodoCerradoAfectado => Periodos.Any(x => x.Cerrado);
    public List<ImpactoPeriodoTraslado> Periodos { get; set; } = [];
}

public sealed class ImpactoPeriodoTraslado
{
    public string Periodo { get; set; } = string.Empty;
    public DateOnly Inicio { get; set; }
    public DateOnly Fin { get; set; }
    public string Escenario { get; set; } = string.Empty;
    public bool Cerrado { get; set; }
    public List<ImpactoCursoTraslado> Cursos { get; set; } = [];
}

public sealed class ImpactoCursoTraslado
{
    public string Curso { get; set; } = string.Empty;
    public string? DocenteOrigen { get; set; }
    public string? DocenteDestino { get; set; }
    public int ActividadesOrigen { get; set; }
    public int ActividadesDestino { get; set; }
    public int EntregasOrigen { get; set; }
    public int EntregasDestino { get; set; }
    public int IntentosOrigen { get; set; }
    public int IntentosDestino { get; set; }
    public int CalificacionesOrigen { get; set; }
    public int CalificacionesDestino { get; set; }
    public bool RequiereResolucionFutura { get; set; }
    public bool PotencialAsignacionDirectaDestino { get; set; }
}

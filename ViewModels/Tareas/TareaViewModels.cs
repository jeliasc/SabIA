using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Proyecto_Final.Models;
using Proyecto_Final.ViewModels.Comunes;

namespace Proyecto_Final.ViewModels.Tareas;

public class FormularioTarea
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una asignación.")]
    public int AsignacionId { get; set; }

    public int? UnidadAsignacionId { get; set; }

    public TipoTarea Tipo { get; set; } =
        TipoTarea.Archivo;

    [Required(ErrorMessage = "El título es obligatorio."), StringLength(200, ErrorMessage = "El título no puede superar los 200 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    public string? Instrucciones { get; set; }

    [Range(typeof(decimal), "0", "9999999", ErrorMessage = "Ingrese un punteo máximo válido.")]
    public decimal PunteoMaximo { get; set; } = 100;

    public DateTime? FechaDisponibilidad { get; set; }

    public DateTime? FechaLimite { get; set; }

    public bool PermitirEntregaTardia { get; set; }

    public int? MaximoIntentosEntrega { get; set; } = 1;

    public CriterioCalificacionIntentos CriterioCalificacionEntrega { get; set; } =
        CriterioCalificacionIntentos.MejorNota;

    public IFormFile? Archivo { get; set; }

    public List<OpcionSeleccion> Asignaciones { get; set; } = [];

    public List<OpcionSeleccion> Unidades { get; set; } = [];
}

public sealed class CrearTarea : FormularioTarea
{
    public Guid TokenCreacion { get; set; }
}

public sealed class EditarTarea : FormularioTarea
{
    public int Id { get; set; }
    public EstadoTarea EstadoActual { get; set; }
    public bool TieneRegistrosAcademicos { get; set; }
    public bool TieneReaperturas { get; set; }
}

public sealed class TareaLista
{
    public int Id { get; set; }

    public int AsignacionId { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Curso { get; set; } = string.Empty;

    public string Seccion { get; set; } = string.Empty;

    public TipoTarea Tipo { get; set; }

    public EstadoTarea Estado { get; set; }

    // No persiste un nuevo estado: describe la disponibilidad calculada para
    // el contexto que consulta la lista.
    public string IndicadorDisponibilidad { get; set; } = string.Empty;

    public DateTime? Limite { get; set; }

    public int Pendientes { get; set; }

    public int Entregadas { get; set; }

    public bool PuedeReabrirse { get; set; }
}

public sealed class ReabrirTarea
{
    public int Id { get; set; }
    public Guid TokenSolicitud { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Curso { get; set; } = string.Empty;
    public TipoTarea Tipo { get; set; }
    public EstadoTarea EstadoActual { get; set; }

    [Required(ErrorMessage = "Seleccione una nueva fecha límite.")]
    public DateTime FechaLimite { get; set; }

    [Required(ErrorMessage = "El motivo es obligatorio."), StringLength(500, MinimumLength = 5)]
    public string Motivo { get; set; } = string.Empty;

    [Required]
    public AlcanceReaperturaTarea Alcance { get; set; } = AlcanceReaperturaTarea.SoloPendientes;
}

public sealed class CerrarTarea
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Curso { get; set; } = string.Empty;
    public TipoTarea Tipo { get; set; }
    public int AlumnosSinActividad { get; set; }
    public bool PuedeCalificarAutomaticamente { get; set; }

    // Se mantiene explícitamente en falso para no asignar ceros por accidente.
    public bool CalificarSinActividad { get; set; }
}

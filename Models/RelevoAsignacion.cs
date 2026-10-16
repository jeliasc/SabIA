using System.ComponentModel.DataAnnotations;

namespace Proyecto_Final.Models;

public enum TipoRelevoAsignacion
{
    Ordinario = 1,
    Excepcional = 2
}

[Flags]
public enum OperacionDelegacionAcademica
{
    Ninguna = 0,
    CalificarEntregas = 1,
    ReabrirEntregas = 2,
    RevisarCuestionarios = 4,
    GestionarEvaluaciones = 8,
    SolicitarCorrecciones = 16,
    ContinuarActividadesAlumnos = 32
}

public sealed class RelevoAsignacion
{
    public int Id { get; set; }
    public int AsignacionAnteriorId { get; set; }
    public Asignacion AsignacionAnterior { get; set; } = null!;
    public int AsignacionNuevaId { get; set; }
    public Asignacion AsignacionNueva { get; set; } = null!;
    public int DocenteAnteriorId { get; set; }
    public Docente DocenteAnterior { get; set; } = null!;
    public int DocenteSustitutoId { get; set; }
    public Docente DocenteSustituto { get; set; } = null!;
    public TipoRelevoAsignacion Tipo { get; set; }
    public DateTime FechaEfectivaUtc { get; set; }
    [Required, StringLength(1000)]
    public string Motivo { get; set; } = string.Empty;
    [Required]
    public string UsuarioResponsableId { get; set; } = string.Empty;
    public Usuario UsuarioResponsable { get; set; } = null!;
    public DateTime FechaRegistroUtc { get; set; }
    [Required]
    public string ObligacionesPendientesJson { get; set; } = "{}";
    public bool ConsecuenciasConfirmadas { get; set; }
    public ICollection<DelegacionAcademica> Delegaciones { get; set; } =
        new List<DelegacionAcademica>();
}

public sealed class DelegacionAcademica
{
    public int Id { get; set; }
    public int RelevoAsignacionId { get; set; }
    public RelevoAsignacion RelevoAsignacion { get; set; } = null!;
    public int AsignacionId { get; set; }
    public Asignacion Asignacion { get; set; } = null!;
    public int DocenteId { get; set; }
    public Docente Docente { get; set; } = null!;
    public OperacionDelegacionAcademica Operaciones { get; set; }
    public DateTime FechaInicioUtc { get; set; }
    public DateTime? FechaFinUtc { get; set; }
    [Required]
    public string CreadaPorUsuarioId { get; set; } = string.Empty;
    public Usuario CreadaPorUsuario { get; set; } = null!;
    public DateTime FechaCreacionUtc { get; set; }
    public bool Activa { get; set; } = true;
}

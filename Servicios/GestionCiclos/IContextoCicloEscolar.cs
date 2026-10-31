using Proyecto_Final.Servicios.Comunes;

namespace Proyecto_Final.Servicios.GestionCiclos;

/// <summary>
/// Contexto de consulta por sesión. No concede permisos ni modifica el estado
/// de un ciclo; los servicios siguen validando autorización y pertenencia.
/// </summary>
public interface IContextoCicloEscolar
{
    Task<ContextoCicloEscolar> ObtenerAsync();
    Task<ResultadoOperacion> SeleccionarAsync(int cicloEscolarId);
    void Restablecer();
}

public sealed class ContextoCicloEscolar
{
    public int? CicloEscolarId { get; init; }
    public int? Anio { get; init; }
    public bool EsActivo { get; init; }
    public bool EsPreparacion { get; init; }
    public bool EsHistorico { get; init; }
    public string? Advertencia { get; init; }
    public IReadOnlyList<OpcionCicloEscolar> Opciones { get; init; } = [];
    public bool Disponible => CicloEscolarId.HasValue;
}

public sealed class OpcionCicloEscolar
{
    public int Id { get; init; }
    public int Anio { get; init; }
    public bool EstaMarcadoActivo { get; init; }
    public bool EsActivo { get; init; }
    public bool EsCerrado { get; init; }
}

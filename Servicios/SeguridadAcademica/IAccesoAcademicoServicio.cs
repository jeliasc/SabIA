namespace Proyecto_Final.Servicios.SeguridadAcademica;

public interface IAccesoAcademicoServicio
{
    string? UsuarioId { get; }
    bool EsOperadorInstitucional { get; }
    bool TienePermiso(string permiso);
    Task<ContextoAcademicoUsuario> ObtenerContextoUsuarioAsync();
    Task<int?> ObtenerDocenteIdAsync();
    Task<int?> ObtenerAlumnoIdAsync();
    Task<bool> EsDocenteDeAsignacionAsync(int asignacionId);
    Task<bool> AlumnoPerteneceAsignacionAsync(int asignacionId);
    Task<bool> PuedeConsultarAsignacionVigenteAsync(int asignacionId);
    Task<bool> PuedeConsultarAsignacionHistoricaAsync(int asignacionId);
    Task<bool> PuedeGestionarAsignacionVigenteAsync(int asignacionId);

    // La consulta contextual de alumnos solo amplía una asignación directa cuando
    // existe una delegación vigente que permite continuar actividades de alumnos.
    // La implementación por defecto es denegatoria para que los dobles de prueba
    // y futuros implementadores no concedan alcance accidentalmente.
    Task<IReadOnlySet<int>> ObtenerSeccionesDelegadasParaConsultaAsync() =>
        Task.FromResult<IReadOnlySet<int>>(new HashSet<int>());
}

public sealed record ContextoAcademicoUsuario(
    int? DocenteId,
    int? AlumnoId)
{
    public bool TienePerfilAcademico => DocenteId.HasValue || AlumnoId.HasValue;
}

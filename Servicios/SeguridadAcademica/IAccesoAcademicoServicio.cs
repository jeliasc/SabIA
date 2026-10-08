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
}

public sealed record ContextoAcademicoUsuario(
    int? DocenteId,
    int? AlumnoId)
{
    public bool TienePerfilAcademico => DocenteId.HasValue || AlumnoId.HasValue;
}

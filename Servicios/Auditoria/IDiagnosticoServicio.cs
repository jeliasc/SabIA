using Proyecto_Final.ViewModels.Auditoria;

namespace Proyecto_Final.Servicios.Auditoria;

public interface IDiagnosticoServicio
{
    Task<DiagnosticoPanel> ObtenerPanelAsync(CancellationToken cancellationToken = default);
    Task<ResultadoDiagnostico> ActivarAsync(SolicitudDiagnostico solicitud, string tecnicoId, CancellationToken cancellationToken = default);
    Task<ResultadoDiagnostico> FinalizarAsync(Guid sesionId, string tecnicoId, CancellationToken cancellationToken = default);
    Task RegistrarSolicitudAsync(DiagnosticoSolicitudTecnica solicitud, CancellationToken cancellationToken = default);
    Task<int> VencerSesionesAsync(CancellationToken cancellationToken = default);
}

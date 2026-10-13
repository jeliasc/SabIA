using Proyecto_Final.ViewModels.Auditoria;

namespace Proyecto_Final.Servicios.Auditoria;

public interface IAuditoriaConsultaServicio
{
    Task<AuditoriaIndice> ObtenerPaginaAsync(AuditoriaFiltro filtro);
    Task<AuditoriaDetalle?> ObtenerDetalleAsync(long id);
}

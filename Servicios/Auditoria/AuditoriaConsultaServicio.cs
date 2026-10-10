using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Seguridad;
using Proyecto_Final.ViewModels.Auditoria;

namespace Proyecto_Final.Servicios.Auditoria;

public sealed class AuditoriaConsultaServicio(
    Contexto contexto,
    IHttpContextAccessor httpContextAccessor) : IAuditoriaConsultaServicio
{
    private const int RegistrosPorPagina = 25;

    public async Task<AuditoriaIndice> ObtenerPaginaAsync(AuditoriaFiltro filtro)
    {
        ExigirSuperusuario();

        filtro.Pagina = Math.Max(1, filtro.Pagina);
        var consulta = contexto.RegistrosAuditoria.AsNoTracking();

        if (filtro.FechaDesde.HasValue)
        {
            var desde = ZonaHorariaAuditoria.InicioDiaGuatemalaEnUtc(filtro.FechaDesde.Value);
            consulta = consulta.Where(x => x.FechaUtc >= desde);
        }

        if (filtro.FechaHasta.HasValue)
        {
            var hastaExclusiva = ZonaHorariaAuditoria.FinExclusivoDiaGuatemalaEnUtc(filtro.FechaHasta.Value);
            consulta = consulta.Where(x => x.FechaUtc < hastaExclusiva);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Responsable))
        {
            var responsable = filtro.Responsable.Trim();
            consulta = consulta.Where(x =>
                x.UsuarioId == responsable ||
                (x.Usuario != null &&
                 ((x.Usuario.UserName != null && x.Usuario.UserName.Contains(responsable)) ||
                  (x.Usuario.Email != null && x.Usuario.Email.Contains(responsable)) ||
                  x.Usuario.PrimerNombre.Contains(responsable) ||
                  x.Usuario.PrimerApellido.Contains(responsable))));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Modulo))
        {
            var modulo = filtro.Modulo.Trim();
            consulta = consulta.Where(x => x.Modulo.Contains(modulo));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Accion))
        {
            var accion = filtro.Accion.Trim();
            consulta = consulta.Where(x => x.Accion.Contains(accion));
        }

        if (!string.IsNullOrWhiteSpace(filtro.DireccionIp))
        {
            var ip = filtro.DireccionIp.Trim();
            consulta = consulta.Where(x => x.DireccionIp != null && x.DireccionIp.Contains(ip));
        }

        if (filtro.Resultado.HasValue)
            consulta = consulta.Where(x => x.Resultado == filtro.Resultado.Value);

        var total = await consulta.CountAsync();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)RegistrosPorPagina));
        filtro.Pagina = Math.Min(filtro.Pagina, totalPaginas);

        var registros = await consulta
            .OrderByDescending(x => x.FechaUtc)
            .ThenByDescending(x => x.Id)
            .Skip((filtro.Pagina - 1) * RegistrosPorPagina)
            .Take(RegistrosPorPagina)
            .Select(x => new AuditoriaFila
            {
                Id = x.Id,
                FechaUtc = x.FechaUtc,
                Responsable = x.Usuario == null
                    ? "Sistema"
                    : (x.Usuario.PrimerNombre + " " + x.Usuario.PrimerApellido),
                Modulo = x.Modulo,
                Accion = x.Accion,
                Resultado = x.Resultado,
                DireccionIp = x.DireccionIp
            })
            .ToListAsync();

        return new AuditoriaIndice
        {
            Filtro = filtro,
            Registros = registros,
            TotalRegistros = total,
            Pagina = filtro.Pagina,
            TotalPaginas = totalPaginas
        };
    }

    public async Task<AuditoriaDetalle?> ObtenerDetalleAsync(long id)
    {
        ExigirSuperusuario();

        return await contexto.RegistrosAuditoria
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new AuditoriaDetalle
            {
                Id = x.Id,
                FechaUtc = x.FechaUtc,
                Responsable = x.Usuario == null
                    ? "Sistema"
                    : (x.Usuario.PrimerNombre + " " + x.Usuario.PrimerApellido),
                UsuarioId = x.UsuarioId,
                Modulo = x.Modulo,
                Accion = x.Accion,
                Tipo = x.Tipo,
                Resultado = x.Resultado,
                Entidad = x.Entidad,
                EntidadId = x.EntidadId,
                Descripcion = x.Descripcion,
                ValoresAnteriores = x.ValoresAnteriores,
                ValoresNuevos = x.ValoresNuevos,
                Justificacion = x.Justificacion,
                DireccionIp = x.DireccionIp,
                UserAgent = x.UserAgent,
                Ruta = x.Ruta,
                MetodoHttp = x.MetodoHttp,
                CorrelationId = x.CorrelationId
            })
            .SingleOrDefaultAsync();
    }

    private void ExigirSuperusuario()
    {
        if (httpContextAccessor.HttpContext?.User.IsInRole(Roles.Superusuario) != true)
            throw new UnauthorizedAccessException("La consulta de auditoría está restringida al Superusuario.");
    }
}

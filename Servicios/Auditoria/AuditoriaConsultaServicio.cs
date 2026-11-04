using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
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
        ExigirAccesoAuditoria();

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

        var totalAplicacion = filtro.Origen == OrigenHistorialAuditoria.BaseDatos
            ? 0 : await consulta.CountAsync();
        var cantidadLectura = RegistrosPorPagina * filtro.Pagina;
        var registros = filtro.Origen == OrigenHistorialAuditoria.BaseDatos
            ? new List<AuditoriaFila>()
            : await consulta
                .OrderByDescending(x => x.FechaUtc).ThenByDescending(x => x.Id)
                .Take(cantidadLectura)
                .Select(x => new AuditoriaFila
                {
                    Id = x.Id, FechaUtc = x.FechaUtc,
                    Responsable = x.Usuario == null ? "Sistema" : (x.Usuario.PrimerNombre + " " + x.Usuario.PrimerApellido),
                    Modulo = x.Modulo, Accion = x.Accion, Resultado = x.Resultado,
                    DireccionIp = x.DireccionIp, Origen = "Aplicación"
                }).ToListAsync();

        var totalBaseDatos = 0;
        if (filtro.Origen != OrigenHistorialAuditoria.Aplicacion && contexto.Database.IsNpgsql())
        {
            var consultaBd = contexto.RegistrosAuditoriaBaseDatos.AsNoTracking();
            if (filtro.FechaDesde.HasValue) consultaBd = consultaBd.Where(x => x.FechaUtc >= ZonaHorariaAuditoria.InicioDiaGuatemalaEnUtc(filtro.FechaDesde.Value));
            if (filtro.FechaHasta.HasValue) consultaBd = consultaBd.Where(x => x.FechaUtc < ZonaHorariaAuditoria.FinExclusivoDiaGuatemalaEnUtc(filtro.FechaHasta.Value));
            if (!string.IsNullOrWhiteSpace(filtro.Responsable)) { var responsable = filtro.Responsable.Trim(); consultaBd = consultaBd.Where(x => x.UsuarioConexion.Contains(responsable)); }
            if (!string.IsNullOrWhiteSpace(filtro.Modulo)) { var modulo = filtro.Modulo.Trim(); consultaBd = consultaBd.Where(x => x.Tabla.Contains(modulo)); }
            if (!string.IsNullOrWhiteSpace(filtro.Accion)) { var accion = filtro.Accion.Trim(); consultaBd = consultaBd.Where(x => x.Operacion.Contains(accion)); }
            // Los triggers no conocen el resultado HTTP; el filtro lo excluye para no inferirlo.
            if (filtro.Resultado.HasValue) consultaBd = consultaBd.Where(_ => false);
            totalBaseDatos = await consultaBd.CountAsync();
            registros.AddRange(await consultaBd.OrderByDescending(x => x.FechaUtc).ThenByDescending(x => x.Id).Take(cantidadLectura)
                .Select(x => new AuditoriaFila
                {
                    Id = x.Id, FechaUtc = x.FechaUtc, Responsable = x.UsuarioConexion,
                    Modulo = x.Tabla, Accion = x.Operacion, Resultado = ResultadoAuditoria.Exitoso,
                    DireccionIp = null, Origen = "Base de datos"
                }).ToListAsync());
        }

        var total = totalAplicacion + totalBaseDatos;
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)RegistrosPorPagina));
        filtro.Pagina = Math.Min(filtro.Pagina, totalPaginas);
        registros = registros.OrderByDescending(x => x.FechaUtc).ThenByDescending(x => x.Id)
            .Skip((filtro.Pagina - 1) * RegistrosPorPagina).Take(RegistrosPorPagina).ToList();

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
        ExigirAccesoAuditoria();

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

    private void ExigirAccesoAuditoria()
    {
        var usuario = httpContextAccessor.HttpContext?.User;

        if (usuario == null ||
            (!usuario.IsInRole(Roles.Superusuario) &&
             !usuario.IsInRole("Técnico")))
        {
            throw new UnauthorizedAccessException(
                "No tiene autorización para consultar los registros de auditoría.");
        }
    }
}

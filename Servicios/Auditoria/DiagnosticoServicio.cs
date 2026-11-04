using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.ViewModels.Auditoria;

namespace Proyecto_Final.Servicios.Auditoria;

public sealed class DiagnosticoServicio(Contexto contexto, IAuditoriaServicio auditoria) : IDiagnosticoServicio
{
    public async Task<DiagnosticoPanel> ObtenerPanelAsync(CancellationToken cancellationToken = default)
    {
        await VencerSesionesAsync(cancellationToken);
        var sesiones = await contexto.SesionesDiagnostico.AsNoTracking()
            .Include(x => x.Usuario).Include(x => x.ActivadaPorUsuario)
            .OrderByDescending(x => x.InicioUtc).Take(50).ToListAsync(cancellationToken);
        var eventos = await contexto.EventosDiagnostico.AsNoTracking()
            .Include(x => x.SesionDiagnostico).ThenInclude(x => x.Usuario)
            .OrderByDescending(x => x.FechaUtc).Take(50).ToListAsync(cancellationToken);
        var modulos = await contexto.RegistrosAuditoria.AsNoTracking()
            .Where(x => !string.IsNullOrWhiteSpace(x.Modulo)).Select(x => x.Modulo)
            .Distinct().OrderBy(x => x).Take(50).ToListAsync(cancellationToken);
        return DiagnosticoPanel.Crear(sesiones, eventos, modulos);
    }

    public async Task<ResultadoDiagnostico> ActivarAsync(SolicitudDiagnostico solicitud, string tecnicoId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tecnicoId) || string.IsNullOrWhiteSpace(solicitud.UsuarioId))
            return ResultadoDiagnostico.Error("Debe seleccionar un usuario válido.");
        if (solicitud.DuracionMinutos is not (5 or 15 or 30))
            return ResultadoDiagnostico.Error("La duración debe ser de 5, 15 o 30 minutos.");
        if (string.IsNullOrWhiteSpace(solicitud.Motivo))
            return ResultadoDiagnostico.Error("Debe indicar el motivo de activación.");

        var usuarioExiste = await contexto.Users.AnyAsync(x => x.Id == solicitud.UsuarioId, cancellationToken);
        if (!usuarioExiste) return ResultadoDiagnostico.Error("El usuario seleccionado ya no existe.");

        await using var transaccion = await contexto.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        await VencerSesionesAsync(cancellationToken);
        var modulo = Limitar(solicitud.Modulo, 80);
        var solapa = await contexto.SesionesDiagnostico.AnyAsync(x => x.UsuarioId == solicitud.UsuarioId &&
            x.Estado == EstadoSesionDiagnostico.Activa &&
            (x.Modulo == null || modulo == null || x.Modulo == modulo), cancellationToken);
        if (solapa) return ResultadoDiagnostico.Error("Ya existe un diagnóstico activo que cubre este usuario y módulo.");

        var inicio = DateTime.UtcNow;
        var sesion = new SesionDiagnostico
        {
            UsuarioId = solicitud.UsuarioId,
            Modulo = modulo,
            DuracionMinutos = solicitud.DuracionMinutos,
            Motivo = Limitar(solicitud.Motivo, 1000)!,
            ActivadaPorUsuarioId = tecnicoId,
            InicioUtc = inicio,
            VencimientoUtc = inicio.AddMinutes(solicitud.DuracionMinutos)
        };
        contexto.SesionesDiagnostico.Add(sesion);
        auditoria.AgregarATransaccion(auditoria.CrearRegistro("Auditoría", "Activar diagnóstico",
            TipoEventoAuditoria.Seguridad, ResultadoAuditoria.Exitoso,
            "Se activó una sesión temporal de diagnóstico.", nameof(SesionDiagnostico), sesion.Id.ToString(),
            justificacion: sesion.Motivo, usuarioId: tecnicoId));
        await contexto.SaveChangesAsync(cancellationToken);
        await transaccion.CommitAsync(cancellationToken);
        return ResultadoDiagnostico.Exito("Diagnóstico activado.");
    }

    public async Task<ResultadoDiagnostico> FinalizarAsync(Guid sesionId, string tecnicoId, CancellationToken cancellationToken = default)
    {
        await VencerSesionesAsync(cancellationToken);
        var sesion = await contexto.SesionesDiagnostico.SingleOrDefaultAsync(x => x.Id == sesionId, cancellationToken);
        if (sesion == null) return ResultadoDiagnostico.Error("La sesión de diagnóstico no existe.");
        if (sesion.Estado != EstadoSesionDiagnostico.Activa) return ResultadoDiagnostico.Error("La sesión ya no está activa.");

        sesion.Estado = EstadoSesionDiagnostico.Finalizada;
        sesion.FinalizadaUtc = DateTime.UtcNow;
        sesion.FinalizadaPorUsuarioId = tecnicoId;
        sesion.MotivoFinalizacion = "Finalizada manualmente.";
        auditoria.AgregarATransaccion(auditoria.CrearRegistro("Auditoría", "Finalizar diagnóstico",
            TipoEventoAuditoria.Seguridad, ResultadoAuditoria.Exitoso,
            "Se finalizó una sesión temporal de diagnóstico.", nameof(SesionDiagnostico), sesion.Id.ToString(),
            usuarioId: tecnicoId));
        await contexto.SaveChangesAsync(cancellationToken);
        return ResultadoDiagnostico.Exito("Diagnóstico finalizado.");
    }

    public async Task RegistrarSolicitudAsync(DiagnosticoSolicitudTecnica solicitud, CancellationToken cancellationToken = default)
    {
        var ahora = DateTime.UtcNow;
        var sesion = await contexto.SesionesDiagnostico.AsNoTracking().Where(x =>
            x.UsuarioId == solicitud.UsuarioId && x.Estado == EstadoSesionDiagnostico.Activa &&
            x.VencimientoUtc > ahora && (x.Modulo == null || x.Modulo == solicitud.Modulo))
            .OrderByDescending(x => x.InicioUtc).FirstOrDefaultAsync(cancellationToken);
        if (sesion == null) return;

        contexto.EventosDiagnostico.Add(new EventoDiagnostico
        {
            SesionDiagnosticoId = sesion.Id, FechaUtc = ahora,
            Modulo = Limitar(solicitud.Modulo, 80) ?? "Sistema",
            Controlador = Limitar(solicitud.Controlador, 120), Accion = Limitar(solicitud.Accion, 120),
            ResultadoHttp = solicitud.ResultadoHttp, Operacion = Limitar(solicitud.Operacion, 120) ?? "Solicitud",
            TipoExcepcion = Limitar(solicitud.TipoExcepcion, 250),
            MensajeTecnico = SanitizarMensaje(solicitud.MensajeTecnico),
            Componente = Limitar(solicitud.Componente, 250), CorrelationId = Limitar(solicitud.CorrelationId, 64),
            DuracionMilisegundos = solicitud.DuracionMilisegundos, Categoria = Limitar(solicitud.Categoria, 80)
        });
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public Task<int> VencerSesionesAsync(CancellationToken cancellationToken = default) =>
        contexto.SesionesDiagnostico.Where(x => x.Estado == EstadoSesionDiagnostico.Activa && x.VencimientoUtc <= DateTime.UtcNow)
            .ExecuteUpdateAsync(x => x.SetProperty(s => s.Estado, EstadoSesionDiagnostico.Vencida)
                .SetProperty(s => s.FinalizadaUtc, DateTime.UtcNow)
                .SetProperty(s => s.MotivoFinalizacion, "Vencimiento automático."), cancellationToken);

    private static string? Limitar(string? valor, int maximo) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim()[..Math.Min(valor.Trim().Length, maximo)];
    private static string? SanitizarMensaje(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        if (valor.Contains("password", StringComparison.OrdinalIgnoreCase) || valor.Contains("contras", StringComparison.OrdinalIgnoreCase) ||
            valor.Contains("token", StringComparison.OrdinalIgnoreCase) || valor.Contains("connection string", StringComparison.OrdinalIgnoreCase))
            return "[Mensaje técnico omitido por seguridad]";
        return Limitar(valor, 2000);
    }
}

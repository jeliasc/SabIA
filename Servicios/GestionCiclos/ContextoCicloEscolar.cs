using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Comunes;

namespace Proyecto_Final.Servicios.GestionCiclos;

public sealed class ContextoCicloEscolarServicio(
    Contexto contexto,
    IHttpContextAccessor httpContextAccessor) : IContextoCicloEscolar
{
    private const string ClaveCiclo = "SabIA.CicloEscolar.Id";
    private const string ClaveUsuario = "SabIA.CicloEscolar.Usuario";

    public async Task<ContextoCicloEscolar> ObtenerAsync()
    {
        var opciones = await contexto.CiclosEscolares.AsNoTracking()
            .OrderByDescending(x => x.Anio)
            .Select(x => new OpcionCicloEscolar
            {
                Id = x.Id,
                Anio = x.Anio,
                EstaMarcadoActivo = x.Activo,
                EsActivo = x.Activo && x.Estado == EstadoCicloEscolar.Activo,
                EsCerrado = x.Estado == EstadoCicloEscolar.Cerrado
            })
            .ToListAsync();

        var marcadosActivos = opciones.Where(x => x.EstaMarcadoActivo).ToList();
        if (marcadosActivos.Count != 1 || !marcadosActivos[0].EsActivo)
        {
            return new ContextoCicloEscolar
            {
                Advertencia = marcadosActivos.Count == 0
                    ? "No existe un ciclo escolar activo configurado."
                    : marcadosActivos.Count > 1
                        ? "Hay más de un ciclo escolar activo; la consulta fue bloqueada hasta corregir la inconsistencia."
                        : "El ciclo escolar marcado como activo tiene un estado inconsistente; la consulta fue bloqueada hasta corregirlo.",
                Opciones = opciones
            };
        }

        var sesion = httpContextAccessor.HttpContext?.Session;
        var usuarioId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var seleccionadoId = sesion?.GetInt32(ClaveCiclo);

        // La cookie de sesión puede sobrevivir al cierre de sesión: nunca se
        // reutiliza una selección perteneciente a otra identidad.
        if (sesion is null || string.IsNullOrWhiteSpace(usuarioId) ||
            !string.Equals(sesion.GetString(ClaveUsuario), usuarioId, StringComparison.Ordinal))
        {
            seleccionadoId = null;
            if (sesion is not null && !string.IsNullOrWhiteSpace(usuarioId))
            {
                sesion.SetString(ClaveUsuario, usuarioId);
                sesion.Remove(ClaveCiclo);
            }
        }

        var seleccionado = opciones.SingleOrDefault(x => x.Id == seleccionadoId) ?? marcadosActivos[0];
        if (sesion is not null && !string.IsNullOrWhiteSpace(usuarioId))
        {
            sesion.SetString(ClaveUsuario, usuarioId);
            sesion.SetInt32(ClaveCiclo, seleccionado.Id);
        }

        return new ContextoCicloEscolar
        {
            CicloEscolarId = seleccionado.Id,
            Anio = seleccionado.Anio,
            EsActivo = seleccionado.EsActivo,
            EsPreparacion = !seleccionado.EsActivo && !seleccionado.EsCerrado,
            EsHistorico = seleccionado.EsCerrado,
            Opciones = opciones
        };
    }

    public async Task<ResultadoOperacion> SeleccionarAsync(int cicloEscolarId)
    {
        var estado = await ObtenerAsync();
        if (!estado.Disponible)
            return ResultadoOperacion.Error(estado.Advertencia ?? "No hay un ciclo escolar disponible.");

        if (!estado.Opciones.Any(x => x.Id == cicloEscolarId))
            return ResultadoOperacion.Error("El ciclo escolar seleccionado no existe.");

        var sesion = httpContextAccessor.HttpContext?.Session;
        var usuarioId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sesion is null || string.IsNullOrWhiteSpace(usuarioId))
            return ResultadoOperacion.Error("No fue posible guardar el contexto de ciclo escolar.");

        sesion.SetString(ClaveUsuario, usuarioId);
        sesion.SetInt32(ClaveCiclo, cicloEscolarId);
        return ResultadoOperacion.Correcto("El ciclo escolar de consulta fue actualizado.");
    }

    public void Restablecer()
    {
        var sesion = httpContextAccessor.HttpContext?.Session;
        sesion?.Remove(ClaveCiclo);
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Proyecto_Final.Servicios.GestionCiclos;

namespace Proyecto_Final.Seguridad.Filtros;

/// <summary>Evita que el contexto histórico se convierta en una vía de escritura ordinaria.</summary>
public sealed class BloquearEscrituraCicloHistoricoFiltro(
    IContextoCicloEscolar contextoCiclo) : IAsyncActionFilter
{
    private static readonly HashSet<string> ControladoresAcademicos = new(StringComparer.OrdinalIgnoreCase)
    {
        "Inscripciones", "Asignaciones", "Secciones", "Periodos", "Unidades",
        "Materiales", "Tareas", "Cuestionarios", "Entregas", "Traslados",
        "Calificaciones", "ComentariosAcademicos"
    };

    public async Task OnActionExecutionAsync(ActionExecutingContext contexto, ActionExecutionDelegate siguiente)
    {
        var solicitud = contexto.HttpContext.Request;
        if (HttpMethods.IsGet(solicitud.Method) || HttpMethods.IsHead(solicitud.Method) ||
            HttpMethods.IsOptions(solicitud.Method))
        {
            await siguiente();
            return;
        }

        var controlador = contexto.RouteData.Values["controller"]?.ToString() ?? string.Empty;
        var accion = contexto.RouteData.Values["action"]?.ToString() ?? string.Empty;
        if (!ControladoresAcademicos.Contains(controlador) ||
            accion.Contains("Correccion", StringComparison.OrdinalIgnoreCase) ||
            accion.Contains("Extraordin", StringComparison.OrdinalIgnoreCase))
        {
            await siguiente();
            return;
        }

        var ciclo = await contextoCiclo.ObtenerAsync();
        if (ciclo.EsHistorico)
        {
            contexto.Result = new ForbidResult();
            return;
        }

        await siguiente();
    }
}

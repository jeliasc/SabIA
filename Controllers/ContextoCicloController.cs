using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Final.Servicios.GestionCiclos;

namespace Proyecto_Final.Controllers;

[Authorize]
public sealed class ContextoCicloController(IContextoCicloEscolar contextoCiclo) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Seleccionar(int cicloEscolarId, string? retorno = null)
    {
        var cicloAnterior = await contextoCiclo.ObtenerAsync();
        var resultado = await contextoCiclo.SeleccionarAsync(cicloEscolarId);
        TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
        var cambioDeCiclo = resultado.Exitoso &&
            cicloAnterior.CicloEscolarId != cicloEscolarId;
        return LocalRedirect(cambioDeCiclo
            ? DestinoCompatible(retorno)
            : EsRetornoLocal(retorno) ? retorno! : "/Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Restablecer(string? retorno = null)
    {
        contextoCiclo.Restablecer();
        return LocalRedirect(DestinoCompatible(retorno));
    }

    private bool EsRetornoLocal(string? retorno) =>
        !string.IsNullOrWhiteSpace(retorno) && Url.IsLocalUrl(retorno);

    // El selector es la única navegación que cambia el contexto. Al hacerlo,
    // un identificador de detalle (asignación, libro, entrega, etc.) puede
    // pertenecer al ciclo previo; por eso se vuelve al índice autorizado del
    // módulo. No interviene en 404 directos ni en solicitudes POST ajenas.
    private string DestinoCompatible(string? retorno)
    {
        if (!EsRetornoLocal(retorno)) return "/Home";

        var ruta = retorno!.Split('?', '#')[0].Trim('/');
        var controlador = ruta.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        return controlador?.ToLowerInvariant() switch
        {
            "alumnos" => "/Alumnos",
            "asignaciones" => "/Asignaciones",
            "calificaciones" => "/Calificaciones",
            "ciclos" => "/Ciclos",
            "contenidoia" => "/ContenidoIA",
            "cuestionarios" => "/Cuestionarios",
            "documentos" => "/Documentos",
            "entregas" => "/Entregas",
            "exploradoracademico" => "/ExploradorAcademico",
            "grados" => "/Grados",
            "home" => "/Home",
            "inscripciones" => "/Inscripciones",
            "materiales" => "/Materiales",
            "periodos" => "/Periodos",
            "planificaciones" => "/Planificaciones",
            "resolucionestraslados" => "/ResolucionesTraslados",
            "secciones" => "/Secciones",
            "tareas" => "/Tareas",
            "unidades" => "/Unidades",
            _ => "/Home"
        };
    }
}

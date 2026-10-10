using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Proyecto_Final.Models;

namespace Proyecto_Final.Servicios.Auditoria;

public sealed class AuditoriaRechazosFiltro(
    IAuditoriaServicio auditoria,
    ILogger<AuditoriaRechazosFiltro> logger) : IAsyncActionFilter
{
    private const string MarcaRegistrado = "Auditoria.RechazoRegistrado";
    private static readonly HashSet<string> OperacionesSensibles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Usuarios.Editar", "Usuarios.Activar", "Usuarios.Desactivar",
        "Usuarios.EnviarRestablecimientoContrasena", "Usuarios.RestablecerContrasenaManual",
        "Roles.Editar", "Roles.Activar", "Roles.Desactivar",
        "PermisosSistema.Crear", "PermisosSistema.Editar",
        "PermisosSistema.Activar", "PermisosSistema.Desactivar",
        "Inscripciones.Crear", "Inscripciones.Trasladar", "Inscripciones.Retirar",
        "Calificaciones.Guardar", "Calificaciones.Cerrar", "Calificaciones.SolicitarCorreccion",
        "Calificaciones.RevisarCorreccion", "Calificaciones.AplicarCorreccion",
        "Planificaciones.EnviarRevision", "Planificaciones.Revisar",
        "Tareas.Publicar", "Tareas.Cerrar", "Entregas.Calificar", "Entregas.Reabrir",
        "Ciclos.Activar", "Ciclos.Cerrar", "Ciclos.Reabrir"
    };

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        ActionExecutedContext ejecutado;
        try
        {
            ejecutado = await next();
        }
        catch (Exception excepcion)
        {
            await RegistrarSeguroAsync(
                context,
                ResultadoAuditoria.Fallido,
                $"La operación terminó con un error no controlado ({excepcion.GetType().Name}).");
            throw;
        }

        if (!EsOperacionDeEscritura(context.HttpContext.Request.Method) ||
            EsAutenticacion(context) ||
            context.HttpContext.Items.ContainsKey(MarcaRegistrado))
            return;

        var codigo = (ejecutado.Result as IStatusCodeActionResult)?.StatusCode;
        var operacionSensibleRechazada = EsOperacionSensible(context) &&
            (!context.ModelState.IsValid ||
             (context.Controller is Controller controlador &&
              (controlador.TempData.ContainsKey("Error") || controlador.TempData.ContainsKey("Advertencia"))));
        if ((codigo is StatusCodes.Status401Unauthorized or
            StatusCodes.Status403Forbidden or
            StatusCodes.Status409Conflict) ||
            operacionSensibleRechazada)
        {
            await RegistrarSeguroAsync(
                context,
                ResultadoAuditoria.Rechazado,
                codigo.HasValue
                    ? $"La operación sensible fue rechazada con estado HTTP {codigo}."
                    : "La operación sensible fue rechazada por una regla de seguridad o de negocio.");
        }
    }

    private async Task RegistrarSeguroAsync(
        ActionExecutingContext context,
        ResultadoAuditoria resultado,
        string descripcion)
    {
        if (context.HttpContext.Items.ContainsKey(MarcaRegistrado)) return;
        context.HttpContext.Items[MarcaRegistrado] = true;

        try
        {
            var controlador = ObtenerRuta(context, "controller", "Sistema");
            var accion = ObtenerRuta(context, "action", "Operación");
            var registro = auditoria.CrearRegistro(
                controlador,
                accion,
                resultado == ResultadoAuditoria.Rechazado
                    ? TipoEventoAuditoria.Seguridad
                    : TipoEventoAuditoria.Operacion,
                resultado,
                descripcion);
            await auditoria.RegistrarIndependienteAsync(registro);
        }
        catch (Exception errorAuditoria)
        {
            logger.LogError(errorAuditoria, "No fue posible persistir el evento rechazado de auditoría.");
        }
    }

    private static bool EsOperacionDeEscritura(string metodo) =>
        HttpMethods.IsPost(metodo) || HttpMethods.IsPut(metodo) ||
        HttpMethods.IsPatch(metodo) || HttpMethods.IsDelete(metodo);

    private static bool EsAutenticacion(ActionExecutingContext context) =>
        string.Equals(
            ObtenerRuta(context, "controller", string.Empty),
            "Login",
            StringComparison.OrdinalIgnoreCase);

    private static bool EsOperacionSensible(ActionExecutingContext context) =>
        OperacionesSensibles.Contains(
            $"{ObtenerRuta(context, "controller", string.Empty)}.{ObtenerRuta(context, "action", string.Empty)}");

    private static string ObtenerRuta(
        ActionExecutingContext context,
        string clave,
        string predeterminado) =>
        context.ActionDescriptor.RouteValues.TryGetValue(clave, out var valor) &&
        !string.IsNullOrWhiteSpace(valor)
            ? valor
            : predeterminado;
}

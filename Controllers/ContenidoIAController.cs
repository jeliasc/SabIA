using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.InteligenciaArtificial;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Comunes;
using Proyecto_Final.ViewModels.ContenidoIA;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Permisos.ContenidoIA.Generar)]
public class ContenidoIAController(
    IIaServicio iaServicio,
    Contexto contexto,
    IAccesoAcademicoServicio acceso) : Controller
{
    public async Task<IActionResult> Index() => View(await PrepararAsync(new GenerarContenidoIa()));

    [HttpGet]
    public async Task<IActionResult> Generar(int? planificacionId)
    {
        var modelo = new GenerarContenidoIa { PlanificacionId = planificacionId ?? 0 };
        return View(await PrepararAsync(modelo));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generar(GenerarContenidoIa modelo, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(await PrepararAsync(modelo));

        var tipos = new List<string>();
        if (modelo.Cuestionario) tipos.Add("cuestionario");
        if (modelo.Resumen) tipos.Add("resumen");
        if (modelo.HojaTrabajo) tipos.Add("hoja_trabajo");
        if (modelo.Glosario) tipos.Add("glosario");

        var resultado = await iaServicio.GenerarAsync(modelo.PlanificacionId, tipos, cancellationToken);
        if (!resultado.Exitoso || resultado.Datos == null)
        {
            ModelState.AddModelError(string.Empty, resultado.Mensaje);
            return View(await PrepararAsync(modelo));
        }

        var nombre = await contexto.Planificaciones.AsNoTracking()
            .Where(x => x.Id == modelo.PlanificacionId)
            .Select(x => x.Titulo)
            .FirstOrDefaultAsync(cancellationToken) ?? "Planificación";

        return View("Trabajos", new TrabajosContenidoIa { Planificacion = nombre, Trabajos = resultado.Datos });
    }

    [HttpGet]
    public async Task<IActionResult> Estado(string id, CancellationToken cancellationToken)
    {
        var resultado = await iaServicio.ConsultarEstadoAsync(id, cancellationToken);
        return Json(new
        {
            exitoso = resultado.Exitoso,
            mensaje = resultado.Mensaje,
            estado = resultado.Datos?.Estado,
            resultado = resultado.Datos?.Resultado
        });
    }

    private async Task<GenerarContenidoIa> PrepararAsync(GenerarContenidoIa modelo)
    {
        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        var consulta = contexto.Planificaciones.AsNoTracking()
            .Where(x =>
                x.Archivos.Any(a => a.Archivo.TipoMime == "application/pdf") &&
                x.Asignaciones.Any(a =>
                    a.Asignacion.Estado == Proyecto_Final.Models.EstadoRegistro.Activo &&
                    a.Asignacion.Seccion.CicloEscolar.Activo));
        if (perfil.DocenteId.HasValue)
            consulta = consulta.Where(x => x.DocenteId == perfil.DocenteId.Value);
        else if (perfil.AlumnoId.HasValue)
            consulta = consulta.Where(_ => false);

        modelo.Planificaciones = await consulta
            .OrderByDescending(x => x.FechaCreacion)
            .Select(x => new OpcionSeleccion { Id = x.Id, Texto = x.Titulo + " · " + x.Estado })
            .ToListAsync();
        return modelo;
    }
}

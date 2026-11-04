using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.ViewModels.Comunes;

namespace Proyecto_Final.Servicios.Academico;

/// <summary>
/// Fuente única de catálogos y comprobación de la jerarquía de inscripción.
/// No concede permisos: cada servicio mantiene su propia autorización.
/// </summary>
public static class SeleccionAcademicaCatalogo
{
    public static async Task PrepararAsync(Contexto contexto, ISeleccionAcademica modelo, int? cicloInicialId = null)
    {
        modelo.CiclosEscolares = await contexto.CiclosEscolares.AsNoTracking()
            .Where(ciclo => ciclo.Activo && ciclo.Estado == EstadoCicloEscolar.Activo)
            .OrderByDescending(ciclo => ciclo.Anio)
            .Select(ciclo => new OpcionSeleccion { Id = ciclo.Id, Texto = ciclo.Anio.ToString() })
            .ToListAsync();

        if (!modelo.CiclosEscolares.Any(ciclo => ciclo.Id == modelo.CicloEscolarId))
            modelo.CicloEscolarId = modelo.CiclosEscolares.Any(ciclo => ciclo.Id == cicloInicialId)
                ? cicloInicialId!.Value : 0;

        var secciones = contexto.Secciones.AsNoTracking().Where(seccion =>
            seccion.Estado == EstadoRegistro.Activo &&
            seccion.CicloEscolar.Activo &&
            seccion.CicloEscolar.Estado == EstadoCicloEscolar.Activo);

        modelo.Carreras = await secciones.Select(seccion => new { seccion.CicloEscolarId, seccion.Grado.Carrera })
            .Distinct().OrderBy(item => item.CicloEscolarId).ThenBy(item => item.Carrera)
            .Select(item => new OpcionCarreraAcademica
            {
                CicloEscolarId = item.CicloEscolarId, Valor = item.Carrera, Texto = item.Carrera
            }).ToListAsync();

        modelo.Grados = await secciones.Select(seccion => new
            {
                seccion.GradoId, seccion.CicloEscolarId, seccion.Grado.Carrera,
                seccion.Grado.Nombre, seccion.Grado.Orden
            }).Distinct().OrderBy(item => item.CicloEscolarId).ThenBy(item => item.Carrera)
            .ThenBy(item => item.Orden).ThenBy(item => item.Nombre)
            .Select(item => new OpcionGradoAcademico
            {
                Id = item.GradoId, CicloEscolarId = item.CicloEscolarId,
                Carrera = item.Carrera, Texto = item.Nombre
            }).ToListAsync();

        modelo.SeccionesAcademicas = await secciones.OrderByDescending(item => item.CicloEscolar.Anio)
            .ThenBy(item => item.Grado.Carrera).ThenBy(item => item.Grado.Orden).ThenBy(item => item.Nombre)
            .Select(item => new OpcionSeccionAcademica
            {
                Id = item.Id, CicloEscolarId = item.CicloEscolarId,
                Carrera = item.Grado.Carrera, GradoId = item.GradoId, Texto = item.Nombre
            }).ToListAsync();
    }

    public static Task<Seccion?> ObtenerSeccionValidaAsync(Contexto contexto, ISeleccionAcademica modelo) =>
        contexto.Secciones.AsNoTracking().Include(seccion => seccion.CicloEscolar)
            .FirstOrDefaultAsync(seccion =>
                seccion.Id == modelo.SeccionId &&
                seccion.CicloEscolarId == modelo.CicloEscolarId &&
                seccion.GradoId == modelo.GradoId &&
                seccion.Grado.Carrera == modelo.Carrera.Trim() &&
                seccion.Estado == EstadoRegistro.Activo &&
                seccion.CicloEscolar.Activo &&
                seccion.CicloEscolar.Estado == EstadoCicloEscolar.Activo);
}

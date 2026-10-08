using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Cursos;

namespace Proyecto_Final.Servicios.GestionCursos;

public sealed class CursoServicio(Contexto contexto) : ICursoServicio
{
    public Task<List<CursoLista>> ObtenerTodosAsync() =>
        contexto.Cursos.AsNoTracking().OrderBy(x => x.Grado.Orden).ThenBy(x => x.Nombre)
            .Select(x => new CursoLista
            {
                Id = x.Id, Nombre = x.Nombre, Grado = x.Grado.Nombre, Carrera = x.Grado.Carrera,
                Estado = x.Estado, Asignaciones = contexto.Asignaciones.Count(a => a.CursoId == x.Id && a.Estado == EstadoRegistro.Activo)
            }).ToListAsync();

    public async Task<ResultadoOperacion> CrearAsync(CrearCurso modelo)
    {
        Normalizar(modelo); var error = await ValidarAsync(modelo, null); if (error != null) return error;
        await using var tx = await contexto.Database.BeginTransactionAsync();
        try
        {
            contexto.Cursos.Add(new Curso { GradoId = modelo.GradoId, Nombre = modelo.Nombre, Estado = EstadoRegistro.Activo });
            await contexto.SaveChangesAsync(); await tx.CommitAsync();
            return ResultadoOperacion.Correcto("El curso fue creado correctamente.");
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public Task<EditarCurso?> ObtenerParaEditarAsync(int id) =>
        contexto.Cursos.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new EditarCurso { Id = x.Id, GradoId = x.GradoId, Nombre = x.Nombre }).FirstOrDefaultAsync();

    public async Task<ResultadoOperacion> EditarAsync(EditarCurso modelo)
    {
        Normalizar(modelo); var error = await ValidarAsync(modelo, modelo.Id); if (error != null) return error;
        var entidad = await contexto.Cursos.FirstOrDefaultAsync(x => x.Id == modelo.Id);
        if (entidad == null) return ResultadoOperacion.Error("El curso no existe.");
        if (entidad.GradoId != modelo.GradoId && await contexto.Asignaciones.AnyAsync(x => x.CursoId == modelo.Id))
            return ResultadoOperacion.Error("No puede cambiarse el grado de un curso que ya tiene asignaciones.");

        await using var tx = await contexto.Database.BeginTransactionAsync();
        try
        {
            entidad.GradoId = modelo.GradoId; entidad.Nombre = modelo.Nombre;
            await contexto.SaveChangesAsync(); await tx.CommitAsync();
            return ResultadoOperacion.Correcto("El curso fue actualizado correctamente.");
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task<ResultadoOperacion> CambiarEstadoAsync(int id, bool activo)
    {
        var entidad = await contexto.Cursos.FirstOrDefaultAsync(x => x.Id == id);
        if (entidad == null) return ResultadoOperacion.Error("El curso no existe.");
        var nuevo = activo ? EstadoRegistro.Activo : EstadoRegistro.Inactivo;
        if (entidad.Estado == nuevo) return ResultadoOperacion.Error(activo ? "El curso ya está activo." : "El curso ya está inactivo.");
        if (!activo && await contexto.Asignaciones.AnyAsync(x => x.CursoId == id && x.Estado == EstadoRegistro.Activo))
            return ResultadoOperacion.Error("No puede desactivarse un curso con asignaciones activas.");

        await using var tx = await contexto.Database.BeginTransactionAsync();
        try
        {
            entidad.Estado = nuevo; await contexto.SaveChangesAsync(); await tx.CommitAsync();
            return ResultadoOperacion.Correcto(activo ? "El curso fue activado correctamente." : "El curso fue desactivado correctamente.");
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    private async Task<ResultadoOperacion?> ValidarAsync(FormularioCurso modelo, int? id)
    {
        if (!await contexto.Grados.AnyAsync(x => x.Id == modelo.GradoId))
            return ResultadoOperacion.Validacion(nameof(modelo.GradoId), "El grado seleccionado no existe.");
        if (await contexto.Cursos.AnyAsync(x => x.GradoId == modelo.GradoId && x.Nombre == modelo.Nombre &&
            (!id.HasValue || x.Id != id.Value)))
            return ResultadoOperacion.Validacion(nameof(modelo.Nombre), "Ya existe un curso con ese nombre para el grado seleccionado.");
        return null;
    }

    private static void Normalizar(FormularioCurso modelo) => modelo.Nombre = modelo.Nombre.Trim();
}

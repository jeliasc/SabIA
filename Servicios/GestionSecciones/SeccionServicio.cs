using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Secciones;

namespace Proyecto_Final.Servicios.GestionSecciones;

public sealed class SeccionServicio(Contexto contexto) : ISeccionServicio
{
    public Task<List<SeccionLista>> ObtenerTodosAsync() =>
        contexto.Secciones.AsNoTracking()
            .OrderByDescending(x => x.CicloEscolar.Anio).ThenBy(x => x.Grado.Orden).ThenBy(x => x.Nombre)
            .Select(x => new SeccionLista
            {
                Id = x.Id,
                Ciclo = x.CicloEscolar.Anio,
                Grado = x.Grado.Nombre + (x.Grado.Carrera == "-" ? "" : " · " + x.Grado.Carrera),
                Nombre = x.Nombre,
                Estado = x.Estado,
                Alumnos = contexto.Inscripciones.Count(i => i.SeccionId == x.Id && i.Estado == EstadoInscripcion.Activa),
                Asignaciones = contexto.Asignaciones.Count(a => a.SeccionId == x.Id && a.Estado == EstadoRegistro.Activo)
            }).ToListAsync();

    public async Task<ResultadoOperacion> CrearAsync(CrearSeccion modelo)
    {
        Normalizar(modelo); var error = await ValidarAsync(modelo, null); if (error != null) return error;
        await using var tx = await contexto.Database.BeginTransactionAsync();
        try
        {
            contexto.Secciones.Add(new Seccion { CicloEscolarId = modelo.CicloEscolarId, GradoId = modelo.GradoId, Nombre = modelo.Nombre, Estado = EstadoRegistro.Activo });
            await contexto.SaveChangesAsync(); await tx.CommitAsync();
            return ResultadoOperacion.Correcto("La sección fue creada correctamente.");
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public Task<EditarSeccion?> ObtenerParaEditarAsync(int id) =>
        contexto.Secciones.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new EditarSeccion { Id = x.Id, CicloEscolarId = x.CicloEscolarId, GradoId = x.GradoId, Nombre = x.Nombre })
            .FirstOrDefaultAsync();

    public async Task<ResultadoOperacion> EditarAsync(EditarSeccion modelo)
    {
        Normalizar(modelo); var error = await ValidarAsync(modelo, modelo.Id); if (error != null) return error;
        var entidad = await contexto.Secciones.FirstOrDefaultAsync(x => x.Id == modelo.Id);
        if (entidad == null) return ResultadoOperacion.Error("La sección no existe.");
        var tieneMovimientos = await contexto.Inscripciones.AnyAsync(x => x.SeccionId == modelo.Id) ||
                              await contexto.Asignaciones.AnyAsync(x => x.SeccionId == modelo.Id);
        if (tieneMovimientos && (entidad.CicloEscolarId != modelo.CicloEscolarId || entidad.GradoId != modelo.GradoId))
            return ResultadoOperacion.Error("No se puede cambiar el ciclo o grado porque la sección ya tiene historial académico.");

        await using var tx = await contexto.Database.BeginTransactionAsync();
        try
        {
            entidad.CicloEscolarId = modelo.CicloEscolarId; entidad.GradoId = modelo.GradoId; entidad.Nombre = modelo.Nombre;
            await contexto.SaveChangesAsync(); await tx.CommitAsync();
            return ResultadoOperacion.Correcto("La sección fue actualizada correctamente.");
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task<ResultadoOperacion> CambiarEstadoAsync(int id, bool activa)
    {
        var entidad = await contexto.Secciones.FirstOrDefaultAsync(x => x.Id == id);
        if (entidad == null) return ResultadoOperacion.Error("La sección no existe.");
        var nuevo = activa ? EstadoRegistro.Activo : EstadoRegistro.Inactivo;
        if (entidad.Estado == nuevo) return ResultadoOperacion.Error(activa ? "La sección ya está activa." : "La sección ya está inactiva.");
        if (!activa && (await contexto.Inscripciones.AnyAsync(x => x.SeccionId == id && x.Estado == EstadoInscripcion.Activa) ||
                        await contexto.Asignaciones.AnyAsync(x => x.SeccionId == id && x.Estado == EstadoRegistro.Activo)))
            return ResultadoOperacion.Error("No puede desactivarse una sección con inscripciones o asignaciones activas.");

        await using var tx = await contexto.Database.BeginTransactionAsync();
        try
        {
            entidad.Estado = nuevo; await contexto.SaveChangesAsync(); await tx.CommitAsync();
            return ResultadoOperacion.Correcto(activa ? "La sección fue activada correctamente." : "La sección fue desactivada correctamente.");
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    private async Task<ResultadoOperacion?> ValidarAsync(FormularioSeccion modelo, int? id)
    {
        if (!await contexto.CiclosEscolares.AnyAsync(x => x.Id == modelo.CicloEscolarId))
            return ResultadoOperacion.Validacion(nameof(modelo.CicloEscolarId), "El ciclo escolar seleccionado no existe.");
        if (!await contexto.Grados.AnyAsync(x => x.Id == modelo.GradoId))
            return ResultadoOperacion.Validacion(nameof(modelo.GradoId), "El grado seleccionado no existe.");
        if (await contexto.Secciones.AnyAsync(x => x.CicloEscolarId == modelo.CicloEscolarId && x.GradoId == modelo.GradoId &&
            x.Nombre == modelo.Nombre && (!id.HasValue || x.Id != id.Value)))
            return ResultadoOperacion.Validacion(nameof(modelo.Nombre), "Ya existe una sección con ese nombre para el grado y ciclo seleccionados.");
        return null;
    }

    private static void Normalizar(FormularioSeccion modelo) => modelo.Nombre = modelo.Nombre.Trim().ToUpperInvariant();
}

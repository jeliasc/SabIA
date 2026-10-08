using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Grados;

namespace Proyecto_Final.Servicios.GestionGrados;

public sealed class GradoServicio(Contexto contexto) : IGradoServicio
{
    public Task<List<GradoLista>> ObtenerTodosAsync() =>
        contexto.Grados.AsNoTracking().OrderBy(x => x.Nivel).ThenBy(x => x.Orden)
            .Select(x => new GradoLista
            {
                Id = x.Id, Nivel = x.Nivel, Nombre = x.Nombre, Carrera = x.Carrera,
                Orden = x.Orden, Secciones = x.Secciones.Count, Cursos = x.Cursos.Count
            }).ToListAsync();

    public async Task<ResultadoOperacion> CrearAsync(CrearGrado modelo)
    {
        Normalizar(modelo);
        var error = await ValidarAsync(modelo, null);
        if (error != null) return error;
        await using var tx = await contexto.Database.BeginTransactionAsync();
        try
        {
            contexto.Grados.Add(new Grado { Nivel = modelo.Nivel, Nombre = modelo.Nombre, Carrera = modelo.Carrera, Orden = modelo.Orden });
            await contexto.SaveChangesAsync(); await tx.CommitAsync();
            return ResultadoOperacion.Correcto("El grado fue creado correctamente.");
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public Task<EditarGrado?> ObtenerParaEditarAsync(int id) =>
        contexto.Grados.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new EditarGrado { Id = x.Id, Nivel = x.Nivel, Nombre = x.Nombre, Carrera = x.Carrera, Orden = x.Orden })
            .FirstOrDefaultAsync();

    public async Task<ResultadoOperacion> EditarAsync(EditarGrado modelo)
    {
        Normalizar(modelo);
        var error = await ValidarAsync(modelo, modelo.Id);
        if (error != null) return error;
        var entidad = await contexto.Grados.FirstOrDefaultAsync(x => x.Id == modelo.Id);
        if (entidad == null) return ResultadoOperacion.Error("El grado no existe.");
        await using var tx = await contexto.Database.BeginTransactionAsync();
        try
        {
            entidad.Nivel = modelo.Nivel; entidad.Nombre = modelo.Nombre; entidad.Carrera = modelo.Carrera; entidad.Orden = modelo.Orden;
            await contexto.SaveChangesAsync(); await tx.CommitAsync();
            return ResultadoOperacion.Correcto("El grado fue actualizado correctamente.");
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    private async Task<ResultadoOperacion?> ValidarAsync(FormularioGrado modelo, int? id)
    {
        if (await contexto.Grados.AnyAsync(x => x.Nivel == modelo.Nivel && x.Nombre == modelo.Nombre &&
            x.Carrera == modelo.Carrera && (!id.HasValue || x.Id != id.Value)))
            return ResultadoOperacion.Validacion(nameof(modelo.Nombre), "Ya existe ese grado para la carrera indicada.");
        return null;
    }

    private static void Normalizar(FormularioGrado modelo)
    {
        modelo.Nombre = modelo.Nombre.Trim();
        modelo.Carrera = modelo.Carrera.Trim();
    }
}

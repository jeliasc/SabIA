using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Ciclos;

namespace Proyecto_Final.Servicios.GestionCiclos;

public sealed class CicloServicio(Contexto contexto) : ICicloServicio
{
    public Task<List<CicloLista>> ObtenerTodosAsync() =>
        contexto.CiclosEscolares.AsNoTracking()
            .OrderByDescending(x => x.Anio)
            .Select(x => new CicloLista
            {
                Id = x.Id, Anio = x.Anio, Inicio = x.FechaInicio, Fin = x.FechaFin,
                Activo = x.Activo, Periodos = x.Periodos.Count, Secciones = x.Secciones.Count
            }).ToListAsync();

    public async Task<ResultadoOperacion> CrearAsync(CrearCiclo modelo)
    {
        var error = await ValidarAsync(modelo.Anio, modelo.FechaInicio, modelo.FechaFin, null);
        if (error != null) return error;

        await using var tx = await contexto.Database.BeginTransactionAsync();
        try
        {
            contexto.CiclosEscolares.Add(new CicloEscolar
            {
                Anio = modelo.Anio, FechaInicio = modelo.FechaInicio,
                FechaFin = modelo.FechaFin, Activo = false
            });
            await contexto.SaveChangesAsync();
            await tx.CommitAsync();
            return ResultadoOperacion.Correcto("El ciclo escolar fue creado correctamente.");
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public Task<EditarCiclo?> ObtenerParaEditarAsync(int id) =>
        contexto.CiclosEscolares.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new EditarCiclo
            {
                Id = x.Id, Anio = x.Anio, FechaInicio = x.FechaInicio, FechaFin = x.FechaFin
            }).FirstOrDefaultAsync();

    public async Task<ResultadoOperacion> EditarAsync(EditarCiclo modelo)
    {
        var error = await ValidarAsync(modelo.Anio, modelo.FechaInicio, modelo.FechaFin, modelo.Id);
        if (error != null) return error;

        var ciclo = await contexto.CiclosEscolares.FirstOrDefaultAsync(x => x.Id == modelo.Id);
        if (ciclo == null) return ResultadoOperacion.Error("El ciclo escolar no existe.");

        await using var tx = await contexto.Database.BeginTransactionAsync();
        try
        {
            ciclo.Anio = modelo.Anio;
            ciclo.FechaInicio = modelo.FechaInicio;
            ciclo.FechaFin = modelo.FechaFin;
            await contexto.SaveChangesAsync();
            await tx.CommitAsync();
            return ResultadoOperacion.Correcto("El ciclo escolar fue actualizado correctamente.");
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task<ResultadoOperacion> ActivarAsync(int id)
    {
        var ciclo = await contexto.CiclosEscolares.FirstOrDefaultAsync(x => x.Id == id);
        if (ciclo == null) return ResultadoOperacion.Error("El ciclo escolar no existe.");
        if (ciclo.Activo) return ResultadoOperacion.Error("El ciclo escolar ya se encuentra activo.");

        await using var tx = await contexto.Database.BeginTransactionAsync();
        try
        {
            var activos = await contexto.CiclosEscolares.Where(x => x.Activo).ToListAsync();
            foreach (var actual in activos) actual.Activo = false;
            ciclo.Activo = true;
            await contexto.SaveChangesAsync();
            await tx.CommitAsync();
            return ResultadoOperacion.Correcto($"El ciclo {ciclo.Anio} quedó establecido como activo.");
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    private async Task<ResultadoOperacion?> ValidarAsync(int anio, DateOnly inicio, DateOnly fin, int? id)
    {
        if (inicio > fin)
            return ResultadoOperacion.Validacion(nameof(FormularioCiclo.FechaFin), "La fecha de finalización debe ser posterior a la fecha de inicio.");
        if (await contexto.CiclosEscolares.AnyAsync(x => x.Anio == anio && (!id.HasValue || x.Id != id.Value)))
            return ResultadoOperacion.Validacion(nameof(FormularioCiclo.Anio), "Ya existe un ciclo escolar para ese año.");
        return null;
    }
}

using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Periodos;

namespace Proyecto_Final.Servicios.GestionPeriodos;

public sealed class PeriodoServicio(Contexto contexto) : IPeriodoServicio
{
    public Task<List<PeriodoLista>> ObtenerTodosAsync() =>
        contexto.Periodos.AsNoTracking()
            .OrderByDescending(x => x.CicloEscolar.Anio).ThenBy(x => x.Numero)
            .Select(x => new PeriodoLista
            {
                Id = x.Id,
                CicloId = x.CicloEscolarId,
                Ciclo = x.CicloEscolar.Anio,
                Numero = x.Numero,
                Nombre = x.Nombre,
                Inicio = x.FechaInicio,
                Fin = x.FechaFin
            }).ToListAsync();

    public async Task<ResultadoOperacion> CrearAsync(CrearPeriodo modelo)
    {
        Normalizar(modelo);
        await using var tx = await contexto.Database.BeginTransactionAsync(
            IsolationLevel.Serializable);
        try
        {
            var error = await ValidarAsync(modelo, null);
            if (error != null)
            {
                await tx.RollbackAsync();
                return error;
            }

            contexto.Periodos.Add(new Periodo
            {
                CicloEscolarId = modelo.CicloEscolarId,
                Numero = modelo.Numero,
                Nombre = modelo.Nombre,
                FechaInicio = modelo.FechaInicio,
                FechaFin = modelo.FechaFin
            });
            await contexto.SaveChangesAsync(); await tx.CommitAsync();
            return ResultadoOperacion.Correcto("El periodo fue creado correctamente.");
        }
        catch (Exception ex) when (EsConflictoConcurrente(ex))
        {
            await tx.RollbackAsync();
            return ResultadoOperacion.Error(
                "No fue posible guardar el período por un conflicto concurrente. Intente nuevamente.");
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public Task<EditarPeriodo?> ObtenerParaEditarAsync(int id) =>
        contexto.Periodos.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new EditarPeriodo
            {
                Id = x.Id,
                CicloEscolarId = x.CicloEscolarId,
                Numero = x.Numero,
                Nombre = x.Nombre,
                FechaInicio = x.FechaInicio,
                FechaFin = x.FechaFin
            }).FirstOrDefaultAsync();

    public async Task<ResultadoOperacion> EditarAsync(EditarPeriodo modelo)
    {
        Normalizar(modelo);
        await using var tx = await contexto.Database.BeginTransactionAsync(
            IsolationLevel.Serializable);
        try
        {
            var error = await ValidarAsync(modelo, modelo.Id);
            if (error != null)
            {
                await tx.RollbackAsync();
                return error;
            }
            var entidad = await contexto.Periodos.FirstOrDefaultAsync(x => x.Id == modelo.Id);
            if (entidad != null && await contexto.CiclosEscolares.AnyAsync(x =>
                x.Id == entidad.CicloEscolarId && x.Estado == EstadoCicloEscolar.Cerrado))
            {
                await tx.RollbackAsync();
                return ResultadoOperacion.Error("No puede modificarse un período perteneciente a un ciclo cerrado.");
            }
            if (entidad == null)
            {
                await tx.RollbackAsync();
                return ResultadoOperacion.Error("El período no existe.");
            }

            entidad.CicloEscolarId = modelo.CicloEscolarId; entidad.Numero = modelo.Numero;
            entidad.Nombre = modelo.Nombre; entidad.FechaInicio = modelo.FechaInicio; entidad.FechaFin = modelo.FechaFin;
            await contexto.SaveChangesAsync(); await tx.CommitAsync();
            return ResultadoOperacion.Correcto("El periodo fue actualizado correctamente.");
        }
        catch (Exception ex) when (EsConflictoConcurrente(ex))
        {
            await tx.RollbackAsync();
            return ResultadoOperacion.Error(
                "No fue posible guardar el período por un conflicto concurrente. Intente nuevamente.");
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    private async Task<ResultadoOperacion?> ValidarAsync(FormularioPeriodo modelo, int? id)
    {
        if (modelo.FechaInicio > modelo.FechaFin)
            return ResultadoOperacion.Validacion(nameof(modelo.FechaFin), "La fecha de finalización debe ser posterior a la fecha de inicio.");

        var ciclo = await contexto.CiclosEscolares.AsNoTracking().FirstOrDefaultAsync(x => x.Id == modelo.CicloEscolarId);
        if (ciclo?.Estado == EstadoCicloEscolar.Cerrado)
            return ResultadoOperacion.Validacion(nameof(modelo.CicloEscolarId), "No pueden modificarse períodos de un ciclo cerrado.");
        if (ciclo == null)
            return ResultadoOperacion.Validacion(nameof(modelo.CicloEscolarId), "El ciclo escolar seleccionado no existe.");
        if (modelo.FechaInicio < ciclo.FechaInicio || modelo.FechaFin > ciclo.FechaFin)
            return ResultadoOperacion.Validacion(nameof(modelo.FechaFin), "Las fechas del periodo deben estar dentro del ciclo escolar.");
        if (await contexto.Periodos.AnyAsync(x =>
            x.CicloEscolarId == modelo.CicloEscolarId && x.Numero == modelo.Numero &&
            (!id.HasValue || x.Id != id.Value)))
            return ResultadoOperacion.Validacion(nameof(modelo.Numero), "Ese número de periodo ya existe en el ciclo escolar.");

        if (await contexto.Periodos.AnyAsync(x =>
            x.CicloEscolarId == modelo.CicloEscolarId &&
            (!id.HasValue || x.Id != id.Value) &&
            x.Nombre.ToUpper() == modelo.Nombre.ToUpper()))
        {
            return ResultadoOperacion.Validacion(
                nameof(modelo.Nombre),
                "Ya existe un período con ese nombre en el ciclo escolar.");
        }

        var conflicto = await contexto.Periodos.AsNoTracking()
            .Where(x => x.CicloEscolarId == modelo.CicloEscolarId &&
                (!id.HasValue || x.Id != id.Value) &&
                modelo.FechaInicio <= x.FechaFin &&
                modelo.FechaFin >= x.FechaInicio)
            .OrderBy(x => x.FechaInicio)
            .Select(x => new { x.Nombre, x.FechaInicio, x.FechaFin })
            .FirstOrDefaultAsync();

        if (conflicto != null)
            return ResultadoOperacion.Validacion(nameof(modelo.FechaInicio),
                $"Las fechas se superponen con el período {conflicto.Nombre} " +
                $"({conflicto.FechaInicio:dd/MM/yyyy} al {conflicto.FechaFin:dd/MM/yyyy}).");

        return null;
    }

    private static void Normalizar(FormularioPeriodo modelo) => modelo.Nombre = modelo.Nombre.Trim();

    private static bool EsConflictoConcurrente(Exception excepcion) =>
        excepcion is DbUpdateException || excepcion is DbException ||
        excepcion.InnerException is DbUpdateException ||
        excepcion.InnerException is DbException;
}

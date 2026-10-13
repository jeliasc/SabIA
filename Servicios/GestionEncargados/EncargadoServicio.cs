using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.ViewModels.Encargados;

namespace Proyecto_Final.Servicios.GestionEncargados;

public sealed class EncargadoServicio(Contexto contexto) : IEncargadoServicio
{
    public Task<List<EncargadoLista>> ObtenerTodosAsync() =>
        contexto.Encargados.AsNoTracking()
            .OrderBy(x => x.Nombres).ThenBy(x => x.Apellidos)
            .Select(x => new EncargadoLista
            {
                Id = x.Id,
                NombreCompleto = (x.Nombres + " " + x.Apellidos).Trim(),
                Dpi = x.Dpi,
                Telefono = x.Telefono,
                Correo = x.Correo,
                Alumnos = x.AlumnoEncargados.Count
            }).ToListAsync();

    public async Task<ResultadoOperacion> CrearAsync(CrearEncargado modelo)
    {
        Normalizar(modelo);
        var validacion = await ValidarAsync(modelo, null);
        if (validacion != null) return validacion;

        await using var transaccion = await contexto.Database.BeginTransactionAsync();
        try
        {
            contexto.Encargados.Add(new Encargado
            {
                Nombres = modelo.Nombres,
                Apellidos = modelo.Apellidos,
                Dpi = modelo.Dpi,
                Telefono = modelo.Telefono,
                TelefonoAlterno = modelo.TelefonoAlterno,
                Correo = modelo.Correo,
                Direccion = modelo.Direccion,
                FechaCreacion = DateTime.UtcNow
            });
            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
            return ResultadoOperacion.Correcto("El encargado fue creado correctamente.");
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    public Task<EditarEncargado?> ObtenerParaEditarAsync(int id) =>
        contexto.Encargados.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new EditarEncargado
            {
                Id = x.Id,
                Nombres = x.Nombres,
                Apellidos = x.Apellidos,
                Dpi = x.Dpi,
                Telefono = x.Telefono,
                TelefonoAlterno = x.TelefonoAlterno,
                Correo = x.Correo,
                Direccion = x.Direccion
            }).FirstOrDefaultAsync();

    public async Task<ResultadoOperacion> EditarAsync(EditarEncargado modelo)
    {
        Normalizar(modelo);
        var validacion = await ValidarAsync(modelo, modelo.Id);
        if (validacion != null) return validacion;

        var encargado = await contexto.Encargados.FirstOrDefaultAsync(x => x.Id == modelo.Id);
        if (encargado == null) return ResultadoOperacion.Error("El encargado seleccionado no existe.");

        await using var transaccion = await contexto.Database.BeginTransactionAsync();
        try
        {
            encargado.Nombres = modelo.Nombres;
            encargado.Apellidos = modelo.Apellidos;
            encargado.Dpi = modelo.Dpi;
            encargado.Telefono = modelo.Telefono;
            encargado.TelefonoAlterno = modelo.TelefonoAlterno;
            encargado.Correo = modelo.Correo;
            encargado.Direccion = modelo.Direccion;
            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
            return ResultadoOperacion.Correcto("El encargado fue actualizado correctamente.");
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    private async Task<ResultadoOperacion?> ValidarAsync(FormularioEncargado modelo, int? id)
    {
        if (!string.IsNullOrWhiteSpace(modelo.Dpi) &&
            await contexto.Encargados.AnyAsync(x =>
                x.Dpi == modelo.Dpi && (!id.HasValue || x.Id != id.Value)))
        {
            return ResultadoOperacion.Validacion(nameof(modelo.Dpi), "Ya existe un encargado con ese DPI.");
        }

        return null;
    }

    private static void Normalizar(FormularioEncargado modelo)
    {
        modelo.Nombres = modelo.Nombres.Trim();
        modelo.Apellidos = modelo.Apellidos.Trim();
        modelo.Dpi = Limpiar(modelo.Dpi);
        modelo.Telefono = modelo.Telefono.Trim();
        modelo.TelefonoAlterno = Limpiar(modelo.TelefonoAlterno);
        modelo.Correo = Limpiar(modelo.Correo)?.ToLowerInvariant();
        modelo.Direccion = Limpiar(modelo.Direccion);
    }

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}

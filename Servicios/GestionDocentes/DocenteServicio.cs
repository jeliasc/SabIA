using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.Academico;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Docentes;
using Proyecto_Final.ViewModels.Usuarios;

namespace Proyecto_Final.Servicios.GestionDocentes;

public sealed class DocenteServicio : IDocenteServicio
{
    private readonly Contexto contexto;
    private readonly ICuentaAcademicaServicio cuentaAcademica;
    private readonly IAccesoAcademicoServicio acceso;

    public DocenteServicio(
        Contexto contexto,
        ICuentaAcademicaServicio cuentaAcademica,
        IAccesoAcademicoServicio acceso)
    {
        this.contexto = contexto;
        this.cuentaAcademica = cuentaAcademica;
        this.acceso = acceso;
    }

    public async Task<List<DocenteLista>> ObtenerTodosAsync()
    {
        if (!acceso.EsOperadorInstitucional)
        {
            return [];
        }

        return await contexto.Docentes
            .AsNoTracking()
            .OrderBy(docente => docente.Usuario.PrimerNombre)
            .ThenBy(docente => docente.Usuario.PrimerApellido)
            .Select(docente => new DocenteLista
            {
                Id = docente.Id,
                Carnet = docente.Carnet,
                NombreCompleto =
                    (docente.Usuario.PrimerNombre + " " +
                     (docente.Usuario.SegundoNombre ?? "") + " " +
                     (docente.Usuario.TercerNombre ?? "") + " " +
                     docente.Usuario.PrimerApellido + " " +
                     (docente.Usuario.SegundoApellido ?? "")).Trim(),
                Correo = docente.Usuario.Email ?? string.Empty,
                NivelAcademico = docente.NivelAcademico,
                Titulo = docente.Titulo,
                Activo = docente.Usuario.Activo,
                AsignacionesActivas = contexto.Asignaciones.Count(
                    asignacion =>
                        asignacion.DocenteId == docente.Id &&
                        asignacion.Estado == EstadoRegistro.Activo
                )
            })
            .ToListAsync();
    }

    public async Task<DetalleDocente?> ObtenerDetalleAsync(int id)
    {
        if (!acceso.EsOperadorInstitucional)
        {
            return null;
        }

        var modelo = await contexto.Docentes
            .AsNoTracking()
            .Where(docente => docente.Id == id)
            .Select(docente => new DetalleDocente
            {
                Id = docente.Id,
                PrimerNombre = docente.Usuario.PrimerNombre,
                SegundoNombre = docente.Usuario.SegundoNombre,
                TercerNombre = docente.Usuario.TercerNombre,
                PrimerApellido = docente.Usuario.PrimerApellido,
                SegundoApellido = docente.Usuario.SegundoApellido,
                Usuario = docente.Usuario.UserName ?? string.Empty,
                Correo = docente.Usuario.Email ?? string.Empty,
                Carnet = docente.Carnet,
                Nit = docente.Nit,
                NivelAcademico = docente.NivelAcademico,
                Titulo = docente.Titulo,
                InstitucionOtorgante = docente.InstitucionOtorgante,
                Activo = docente.Usuario.Activo,
                CambiarContrasena = docente.Usuario.CambiarContrasena,
                FechaCreacion = docente.Usuario.FechaCreacion,
                FechaUltimoCambioContrasena =
                    docente.Usuario.FechaUltimoCambioContrasena
            })
            .FirstOrDefaultAsync();

        if (modelo == null)
        {
            return null;
        }

        modelo.Asignaciones = await contexto.Asignaciones
            .AsNoTracking()
            .Where(asignacion => asignacion.DocenteId == id)
            .OrderByDescending(asignacion =>
                asignacion.Seccion.CicloEscolar.Anio)
            .ThenBy(asignacion => asignacion.Seccion.Grado.Orden)
            .ThenBy(asignacion => asignacion.Curso.Nombre)
            .ThenBy(asignacion => asignacion.Seccion.Nombre)
            .Select(asignacion => new AsignacionDocenteDetalle
            {
                Id = asignacion.Id,
                AnioCicloEscolar = asignacion.Seccion.CicloEscolar.Anio,
                Grado = asignacion.Seccion.Grado.Nombre,
                Seccion = asignacion.Seccion.Nombre,
                Carrera = asignacion.Seccion.Grado.Carrera,
                Curso = asignacion.Curso.Nombre,
                Estado = asignacion.Estado
            })
            .ToListAsync();

        return modelo;
    }

    public async Task<PerfilDocentePropio?> ObtenerPerfilPropioAsync()
    {
        var docenteId = await acceso.ObtenerDocenteIdAsync();
        if (!docenteId.HasValue)
        {
            return null;
        }

        var modelo = await contexto.Docentes.AsNoTracking()
            .Where(docente => docente.Id == docenteId.Value && docente.Usuario.Activo)
            .Select(docente => new PerfilDocentePropio
            {
                NombreCompleto = docente.Usuario.PrimerNombre +
                    (docente.Usuario.SegundoNombre == null ? "" : " " + docente.Usuario.SegundoNombre) +
                    (docente.Usuario.TercerNombre == null ? "" : " " + docente.Usuario.TercerNombre) +
                    " " + docente.Usuario.PrimerApellido +
                    (docente.Usuario.SegundoApellido == null ? "" : " " + docente.Usuario.SegundoApellido),
                Correo = docente.Usuario.Email ?? string.Empty,
                Carnet = docente.Carnet,
                Nit = docente.Nit,
                NivelAcademico = docente.NivelAcademico == null
                    ? null : docente.NivelAcademico.ToString(),
                Titulo = docente.Titulo,
                InstitucionOtorgante = docente.InstitucionOtorgante
            })
            .FirstOrDefaultAsync();

        if (modelo == null)
        {
            return null;
        }

        modelo.Asignaciones = await ObtenerAsignacionesContextualesAsync(
            contexto.Asignaciones.AsNoTracking().Where(asignacion =>
                asignacion.DocenteId == docenteId.Value &&
                asignacion.Estado == EstadoRegistro.Activo &&
                asignacion.Curso.Estado == EstadoRegistro.Activo &&
                asignacion.Seccion.Estado == EstadoRegistro.Activo &&
                asignacion.Seccion.CicloEscolar.Activo &&
                asignacion.Seccion.CicloEscolar.Estado == EstadoCicloEscolar.Activo));

        return modelo;
    }

    public async Task<IReadOnlyList<DocenteContextual>?> ObtenerDirectorioContextualAsync()
    {
        var docenteId = await acceso.ObtenerDocenteIdAsync();
        if (!docenteId.HasValue)
        {
            return null;
        }

        return await contexto.Docentes.AsNoTracking()
            .Where(docente => docente.Id != docenteId.Value && docente.Usuario.Activo)
            .OrderBy(docente => docente.Usuario.PrimerNombre)
            .ThenBy(docente => docente.Usuario.PrimerApellido)
            .Select(docente => new DocenteContextual
            {
                NombreCompleto = docente.Usuario.PrimerNombre +
                    (docente.Usuario.SegundoNombre == null ? "" : " " + docente.Usuario.SegundoNombre) +
                    (docente.Usuario.TercerNombre == null ? "" : " " + docente.Usuario.TercerNombre) +
                    " " + docente.Usuario.PrimerApellido +
                    (docente.Usuario.SegundoApellido == null ? "" : " " + docente.Usuario.SegundoApellido)
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyList<DocenteContextual>?> ObtenerDocentesDelAlumnoAsync()
    {
        var alumnoId = await acceso.ObtenerAlumnoIdAsync();
        if (!alumnoId.HasValue)
        {
            return null;
        }

        var ahora = DateTime.UtcNow;
        var filas = await contexto.Asignaciones.AsNoTracking()
            .Where(asignacion =>
                asignacion.Docente.Usuario.Activo &&
                asignacion.Curso.Estado == EstadoRegistro.Activo &&
                asignacion.Seccion.Estado == EstadoRegistro.Activo &&
                asignacion.Seccion.CicloEscolar.Activo &&
                asignacion.Seccion.CicloEscolar.Estado == EstadoCicloEscolar.Activo &&
                (asignacion.Estado == EstadoRegistro.Activo ||
                 contexto.DelegacionesAcademicas.Any(delegacion =>
                    delegacion.AsignacionId == asignacion.Id &&
                    delegacion.Activa &&
                    delegacion.FechaInicioUtc <= ahora &&
                    (!delegacion.FechaFinUtc.HasValue || delegacion.FechaFinUtc >= ahora) &&
                    (delegacion.Operaciones &
                        OperacionDelegacionAcademica.ContinuarActividadesAlumnos) ==
                        OperacionDelegacionAcademica.ContinuarActividadesAlumnos)) &&
                contexto.Inscripciones.Any(inscripcion =>
                    inscripcion.AlumnoId == alumnoId.Value &&
                    inscripcion.SeccionId == asignacion.SeccionId &&
                    inscripcion.CicloEscolarId == asignacion.Seccion.CicloEscolarId &&
                    inscripcion.Estado == EstadoInscripcion.Activa &&
                    inscripcion.Seccion.Estado == EstadoRegistro.Activo &&
                    inscripcion.Seccion.CicloEscolar.Activo &&
                    inscripcion.Seccion.CicloEscolar.Estado == EstadoCicloEscolar.Activo))
            .Select(asignacion => new DocenteAsignacionFila
            {
                DocenteId = asignacion.DocenteId,
                NombreCompleto = asignacion.Docente.Usuario.PrimerNombre +
                    (asignacion.Docente.Usuario.SegundoNombre == null ? "" : " " + asignacion.Docente.Usuario.SegundoNombre) +
                    (asignacion.Docente.Usuario.TercerNombre == null ? "" : " " + asignacion.Docente.Usuario.TercerNombre) +
                    " " + asignacion.Docente.Usuario.PrimerApellido +
                    (asignacion.Docente.Usuario.SegundoApellido == null ? "" : " " + asignacion.Docente.Usuario.SegundoApellido),
                CicloEscolar = asignacion.Seccion.CicloEscolar.Anio,
                Grado = asignacion.Seccion.Grado.Nombre,
                Carrera = asignacion.Seccion.Grado.Carrera,
                Seccion = asignacion.Seccion.Nombre,
                Curso = asignacion.Curso.Nombre
            })
            .ToListAsync();

        return filas.GroupBy(fila => new { fila.DocenteId, fila.NombreCompleto })
            .OrderBy(grupo => grupo.Key.NombreCompleto)
            .Select(grupo => new DocenteContextual
            {
                NombreCompleto = grupo.Key.NombreCompleto,
                Asignaciones = grupo.Select(fila => new AsignacionDocenteContextual
                {
                    CicloEscolar = fila.CicloEscolar,
                    Grado = fila.Grado,
                    Carrera = fila.Carrera,
                    Seccion = fila.Seccion,
                    Curso = fila.Curso
                }).OrderBy(asignacion => asignacion.Curso).ToList()
            })
            .ToList();
    }

    private static async Task<IReadOnlyList<AsignacionDocenteContextual>>
        ObtenerAsignacionesContextualesAsync(IQueryable<Asignacion> consulta) =>
        await consulta.OrderBy(asignacion => asignacion.Seccion.CicloEscolar.Anio)
            .ThenBy(asignacion => asignacion.Seccion.Grado.Orden)
            .ThenBy(asignacion => asignacion.Seccion.Nombre)
            .ThenBy(asignacion => asignacion.Curso.Nombre)
            .Select(asignacion => new AsignacionDocenteContextual
            {
                CicloEscolar = asignacion.Seccion.CicloEscolar.Anio,
                Grado = asignacion.Seccion.Grado.Nombre,
                Carrera = asignacion.Seccion.Grado.Carrera,
                Seccion = asignacion.Seccion.Nombre,
                Curso = asignacion.Curso.Nombre
            }).ToListAsync();

    private sealed class DocenteAsignacionFila
    {
        public int DocenteId { get; init; }
        public string NombreCompleto { get; init; } = string.Empty;
        public int CicloEscolar { get; init; }
        public string Grado { get; init; } = string.Empty;
        public string Carrera { get; init; } = string.Empty;
        public string Seccion { get; init; } = string.Empty;
        public string Curso { get; init; } = string.Empty;
    }

    public async Task<ResultadoOperacion<ResultadoContrasenaTemporal>> CrearAsync(
        CrearDocente modelo)
    {
        if (!acceso.EsOperadorInstitucional)
        {
            return ResultadoOperacion<ResultadoContrasenaTemporal>.Error(
                "No tiene autorización institucional para administrar docentes.");
        }

        modelo.Carnet = modelo.Carnet.Trim();
        modelo.Nit = Limpiar(modelo.Nit);
        modelo.Titulo = Limpiar(modelo.Titulo);
        modelo.InstitucionOtorgante = Limpiar(modelo.InstitucionOtorgante);

        var existeCarnet =
            await ExisteCarnetAsync(modelo.Carnet);

        if (existeCarnet)
        {
            return ResultadoOperacion<ResultadoContrasenaTemporal>.Validacion(
                nameof(modelo.Carnet),
                "Ya existe un docente con ese carnet."
            );
        }

        await using var transaccion =
            await contexto.Database.BeginTransactionAsync();

        try
        {
            var resultadoCuenta =
                await cuentaAcademica.CrearAsync(
                    modelo,
                    Roles.Docente
                );

            if (!resultadoCuenta.Exitoso ||
                resultadoCuenta.Datos == null)
            {
                await transaccion.RollbackAsync();

                return new ResultadoOperacion<ResultadoContrasenaTemporal>
                {
                    Exitoso = false,
                    Mensaje = resultadoCuenta.Mensaje,
                    Errores = resultadoCuenta.Errores
                };
            }

            var cuenta = resultadoCuenta.Datos;

            contexto.Docentes.Add(
                new Docente
                {
                    UsuarioId = cuenta.Usuario.Id,
                    Carnet = modelo.Carnet,
                    Nit = modelo.Nit,
                    NivelAcademico = modelo.NivelAcademico,
                    Titulo = modelo.Titulo,
                    InstitucionOtorgante = modelo.InstitucionOtorgante
                }
            );

            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();

            var mensaje =
                "El docente fue creado correctamente y se generó su cuenta de acceso.";

            try
            {
                await cuentaAcademica.EnviarCredencialesAsync(
                    cuenta.Usuario,
                    cuenta.ContrasenaTemporal
                );

                mensaje += " Las credenciales fueron enviadas por correo.";
            }
            catch
            {
                mensaje += " No fue posible enviar el correo; entregue la contraseña temporal por otro medio.";
            }

            return ResultadoOperacion<ResultadoContrasenaTemporal>.Correcto(
                cuentaAcademica.CrearResultadoCredenciales(
                    cuenta.Usuario,
                    cuenta.ContrasenaTemporal
                ),
                mensaje
            );
        }
        catch (DbUpdateException excepcion)
            when (excepcion.Entries.Any(entrada => entrada.Entity is Docente))
        {
            await transaccion.RollbackAsync();

            if (await ExisteCarnetAsync(modelo.Carnet))
            {
                return ResultadoOperacion<ResultadoContrasenaTemporal>.Validacion(
                    nameof(modelo.Carnet),
                    "Ya existe un docente con ese carnet."
                );
            }

            throw;
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    public async Task<EditarDocente?> ObtenerParaEditarAsync(int id)
    {
        if (!acceso.EsOperadorInstitucional)
        {
            return null;
        }

        return await contexto.Docentes
            .AsNoTracking()
            .Where(docente => docente.Id == id)
            .Select(docente => new EditarDocente
            {
                Id = docente.Id,
                PrimerNombre = docente.Usuario.PrimerNombre,
                SegundoNombre = docente.Usuario.SegundoNombre,
                TercerNombre = docente.Usuario.TercerNombre,
                PrimerApellido = docente.Usuario.PrimerApellido,
                SegundoApellido = docente.Usuario.SegundoApellido,
                Correo = docente.Usuario.Email ?? string.Empty,
                Carnet = docente.Carnet,
                Nit = docente.Nit,
                NivelAcademico = docente.NivelAcademico,
                Titulo = docente.Titulo,
                InstitucionOtorgante = docente.InstitucionOtorgante
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ResultadoOperacion> EditarAsync(
        EditarDocente modelo)
    {
        if (!acceso.EsOperadorInstitucional)
        {
            return ResultadoOperacion.Error("No tiene autorización institucional para administrar docentes.");
        }

        modelo.Carnet = modelo.Carnet.Trim();

        var existeCarnet =
            await ExisteCarnetAsync(modelo.Carnet, modelo.Id);

        if (existeCarnet)
        {
            return ResultadoOperacion.Validacion(
                nameof(modelo.Carnet),
                "Ya existe un docente con ese carnet."
            );
        }

        await using var transaccion =
            await contexto.Database.BeginTransactionAsync();

        try
        {
            var docente =
                await contexto.Docentes
                    .Include(item => item.Usuario)
                    .FirstOrDefaultAsync(item =>
                        item.Id == modelo.Id);

            if (docente == null)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error(
                    "El docente seleccionado no existe."
                );
            }

            var resultadoCuenta =
                await cuentaAcademica.ActualizarAsync(
                    docente.Usuario,
                    modelo
                );

            if (!resultadoCuenta.Exitoso)
            {
                await transaccion.RollbackAsync();
                return resultadoCuenta;
            }

            docente.Carnet = modelo.Carnet;
            docente.Nit = Limpiar(modelo.Nit);
            docente.NivelAcademico = modelo.NivelAcademico;
            docente.Titulo = Limpiar(modelo.Titulo);
            docente.InstitucionOtorgante =
                Limpiar(modelo.InstitucionOtorgante);

            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();

            return ResultadoOperacion.Correcto(
                "El docente fue actualizado correctamente."
            );
        }
        catch (DbUpdateException excepcion)
            when (excepcion.Entries.Any(entrada => entrada.Entity is Docente))
        {
            await transaccion.RollbackAsync();

            if (await ExisteCarnetAsync(modelo.Carnet, modelo.Id))
            {
                return ResultadoOperacion.Validacion(
                    nameof(modelo.Carnet),
                    "Ya existe un docente con ese carnet."
                );
            }

            throw;
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    public async Task<ResultadoOperacion> CambiarEstadoAsync(
        int id,
        bool activo)
    {
        if (!acceso.EsOperadorInstitucional)
        {
            return ResultadoOperacion.Error("No tiene autorización institucional para administrar docentes.");
        }

        await using var transaccion =
            await contexto.Database.BeginTransactionAsync();

        try
        {
            var docente =
                await contexto.Docentes
                    .Include(item => item.Usuario)
                    .FirstOrDefaultAsync(item => item.Id == id);

            if (docente == null)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error(
                    "El docente seleccionado no existe."
                );
            }

            if (docente.Usuario.Activo == activo)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error(
                    activo
                        ? "El docente ya se encuentra activo."
                        : "El docente ya se encuentra inactivo."
                );
            }

            if (!activo)
            {
                var tieneAsignaciones =
                    await contexto.Asignaciones.AnyAsync(asignacion =>
                        asignacion.DocenteId == docente.Id &&
                        asignacion.Estado == EstadoRegistro.Activo);

                if (tieneAsignaciones)
                {
                    await transaccion.RollbackAsync();
                    return ResultadoOperacion.Error(
                        "No puede desactivar al docente mientras tenga asignaciones académicas activas."
                    );
                }
            }

            docente.Usuario.Activo = activo;
            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();

            return ResultadoOperacion.Correcto(
                activo
                    ? "El docente fue activado correctamente."
                    : "El docente fue desactivado correctamente."
            );
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    private Task<bool> ExisteCarnetAsync(
        string carnet,
        int? docenteExcluidoId = null)
    {
        var consulta = contexto.Docentes
            .AsNoTracking()
            .Where(docente => docente.Carnet == carnet);

        if (docenteExcluidoId.HasValue)
        {
            consulta = consulta.Where(docente =>
                docente.Id != docenteExcluidoId.Value);
        }

        return consulta.AnyAsync();
    }

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor)
            ? null
            : valor.Trim();
}

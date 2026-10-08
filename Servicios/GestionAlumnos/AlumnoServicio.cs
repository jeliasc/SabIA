using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.Academico;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.SeguridadAcademica;
using Proyecto_Final.ViewModels.Alumnos;
using Proyecto_Final.ViewModels.Comunes;
using Proyecto_Final.ViewModels.Usuarios;

namespace Proyecto_Final.Servicios.GestionAlumnos;

public sealed class AlumnoServicio : IAlumnoServicio
{
    private readonly Contexto contexto;
    private readonly UserManager<Usuario> administradorUsuarios;
    private readonly ICuentaAcademicaServicio cuentaAcademica;
    private readonly IEntregaPendienteServicio entregaPendienteServicio;
    private readonly IAccesoAcademicoServicio acceso;

    public AlumnoServicio(
        Contexto contexto,
        UserManager<Usuario> administradorUsuarios,
        ICuentaAcademicaServicio cuentaAcademica,
        IEntregaPendienteServicio entregaPendienteServicio,
        IAccesoAcademicoServicio acceso)
    {
        this.contexto = contexto;
        this.administradorUsuarios = administradorUsuarios;
        this.cuentaAcademica = cuentaAcademica;
        this.entregaPendienteServicio = entregaPendienteServicio;
        this.acceso = acceso;
    }

    public async Task<List<AlumnoLista>> ObtenerTodosAsync(
        ClaimsPrincipal usuarioActual)
    {
        var usuarioId = administradorUsuarios.GetUserId(usuarioActual);

        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return [];
        }

        var alumnoActualId = await contexto.Alumnos
            .AsNoTracking()
            .Where(alumno => alumno.UsuarioId == usuarioId)
            .Select(alumno => (int?)alumno.Id)
            .FirstOrDefaultAsync();

        var docenteId = await contexto.Docentes
            .AsNoTracking()
            .Where(docente => docente.UsuarioId == usuarioId)
            .Select(docente => (int?)docente.Id)
            .FirstOrDefaultAsync();

        var consulta = contexto.Alumnos
            .AsNoTracking()
            .AsQueryable();

        if (!acceso.EsOperadorInstitucional && docenteId.HasValue)
        {
            var secciones = contexto.Asignaciones
                .AsNoTracking()
                .Where(asignacion =>
                    asignacion.DocenteId == docenteId.Value &&
                    asignacion.Estado == EstadoRegistro.Activo &&
                    asignacion.Seccion.CicloEscolar.Activo)
                .Select(asignacion => asignacion.SeccionId);

            consulta = consulta.Where(alumno =>
                contexto.Inscripciones.Any(inscripcion =>
                    inscripcion.AlumnoId == alumno.Id &&
                    inscripcion.Estado == EstadoInscripcion.Activa &&
                    inscripcion.Seccion.CicloEscolar.Activo &&
                    secciones.Contains(inscripcion.SeccionId)));
        }
        else if (!acceso.EsOperadorInstitucional && alumnoActualId.HasValue)
        {
            consulta = consulta.Where(alumno => alumno.Id == alumnoActualId.Value);
        }

        return await consulta
            .OrderBy(alumno => alumno.Usuario.PrimerNombre)
            .ThenBy(alumno => alumno.Usuario.PrimerApellido)
            .Select(alumno => new AlumnoLista
            {
                Id = alumno.Id,
                CodigoPersonal = alumno.CodigoPersonal ?? "Sin código",
                NombreCompleto =
                    (alumno.Usuario.PrimerNombre + " " +
                     (alumno.Usuario.SegundoNombre ?? "") + " " +
                     (alumno.Usuario.TercerNombre ?? "") + " " +
                     alumno.Usuario.PrimerApellido + " " +
                     (alumno.Usuario.SegundoApellido ?? "")).Trim(),
                Correo = alumno.Usuario.Email ?? string.Empty,
                GradoSeccion = contexto.Inscripciones
                    .Where(inscripcion =>
                        inscripcion.AlumnoId == alumno.Id &&
                        inscripcion.Estado == EstadoInscripcion.Activa &&
                        inscripcion.Seccion.CicloEscolar.Activo)
                    .Select(inscripcion =>
                        inscripcion.Seccion.Grado.Nombre + " " +
                        inscripcion.Seccion.Nombre)
                    .FirstOrDefault() ?? "Sin inscripción vigente",
                Ciclo = contexto.Inscripciones
                    .Where(inscripcion =>
                        inscripcion.AlumnoId == alumno.Id &&
                        inscripcion.Estado == EstadoInscripcion.Activa &&
                        inscripcion.Seccion.CicloEscolar.Activo)
                    .Select(inscripcion =>
                        (int?)inscripcion.Seccion.CicloEscolar.Anio)
                    .FirstOrDefault(),
                Encargados = alumno.AlumnoEncargados.Count,
                Activo = alumno.Usuario.Activo
            })
            .ToListAsync();
    }

    public async Task<ResultadoOperacion<AlumnoDetalle>> ObtenerDetalleAsync(
        int id,
        ClaimsPrincipal usuarioActual)
    {
        if (!await PuedeConsultarAlumnoAsync(id, usuarioActual))
        {
            return ResultadoOperacion<AlumnoDetalle>.Error(
                "No tiene autorización para consultar este alumno."
            );
        }

        var perfil = await acceso.ObtenerContextoUsuarioAsync();
        var consultaDocente = !acceso.EsOperadorInstitucional &&
            perfil.DocenteId.HasValue;

        var modelo = await contexto.Alumnos
            .AsNoTracking()
            .Where(alumno => alumno.Id == id)
            .Select(alumno => new AlumnoDetalle
            {
                Id = alumno.Id,
                PrimerNombre = alumno.Usuario.PrimerNombre,
                SegundoNombre = alumno.Usuario.SegundoNombre,
                TercerNombre = alumno.Usuario.TercerNombre,
                PrimerApellido = alumno.Usuario.PrimerApellido,
                SegundoApellido = alumno.Usuario.SegundoApellido,
                CodigoPersonal = alumno.CodigoPersonal ?? "Sin código",
                NombreCompleto =
                    (alumno.Usuario.PrimerNombre + " " +
                     (alumno.Usuario.SegundoNombre ?? "") + " " +
                     (alumno.Usuario.TercerNombre ?? "") + " " +
                     alumno.Usuario.PrimerApellido + " " +
                     (alumno.Usuario.SegundoApellido ?? "")).Trim(),
                Usuario = alumno.Usuario.UserName ?? string.Empty,
                Correo = alumno.Usuario.Email ?? string.Empty,
                Telefono = alumno.Usuario.PhoneNumber,
                Activo = alumno.Usuario.Activo,
                FechaCreacion = alumno.Usuario.FechaCreacion,
                Encargados = alumno.AlumnoEncargados
                    .OrderBy(relacion => relacion.Encargado.Nombres)
                    .ThenBy(relacion => relacion.Encargado.Apellidos)
                    .Select(relacion => new EncargadoAlumnoDetalle
                    {
                        NombreCompleto =
                            relacion.Encargado.Nombres + " " +
                            relacion.Encargado.Apellidos,
                        Telefono = relacion.Encargado.Telefono,
                        TelefonoAlterno = relacion.Encargado.TelefonoAlterno,
                        Correo = relacion.Encargado.Correo,
                        Parentesco = relacion.Parentesco.ToString()
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (modelo == null)
        {
            return ResultadoOperacion<AlumnoDetalle>.Error(
                "El alumno seleccionado no existe."
            );
        }

        var consultaInscripciones = contexto.Inscripciones
            .AsNoTracking()
            .Where(inscripcion => inscripcion.AlumnoId == id);

        if (consultaDocente)
        {
            var docenteId = perfil.DocenteId!.Value;
            consultaInscripciones = consultaInscripciones.Where(inscripcion =>
                inscripcion.Estado == EstadoInscripcion.Activa &&
                inscripcion.Seccion.CicloEscolar.Activo &&
                contexto.Asignaciones.Any(asignacion =>
                    asignacion.DocenteId == docenteId &&
                    asignacion.SeccionId == inscripcion.SeccionId &&
                    asignacion.Estado == EstadoRegistro.Activo &&
                    asignacion.Seccion.CicloEscolar.Activo));
        }

        var inscripciones = await consultaInscripciones
            .OrderByDescending(inscripcion =>
                inscripcion.Seccion.CicloEscolar.Anio)
            .ThenByDescending(inscripcion => inscripcion.Fecha)
            .Select(inscripcion => new InscripcionAlumnoDetalle
            {
                Id = inscripcion.Id,
                SeccionId = inscripcion.SeccionId,
                CicloEscolarId = inscripcion.CicloEscolarId,
                Ciclo = inscripcion.Seccion.CicloEscolar.Anio,
                Grado = inscripcion.Seccion.Grado.Nombre,
                Seccion = inscripcion.Seccion.Nombre,
                Carrera = inscripcion.Seccion.Grado.Carrera,
                Fecha = inscripcion.Fecha,
                Estado = inscripcion.Estado,
                EsVigente =
                    inscripcion.Estado == EstadoInscripcion.Activa &&
                    inscripcion.Seccion.CicloEscolar.Activo
            })
            .ToListAsync();

        modelo.Inscripciones = inscripciones;
        modelo.InscripcionVigente = inscripciones
            .FirstOrDefault(inscripcion => inscripcion.EsVigente);

        if (modelo.InscripcionVigente != null)
        {
            var asignaciones = contexto.Asignaciones
                .AsNoTracking()
                .Where(asignacion =>
                    asignacion.SeccionId == modelo.InscripcionVigente.SeccionId &&
                    asignacion.Estado == EstadoRegistro.Activo &&
                    asignacion.Seccion.CicloEscolar.Activo)
                .Select(asignacion => asignacion.Id);

            modelo.UnidadesDisponibles =
                await contexto.UnidadesAsignaciones.CountAsync(unidad =>
                    asignaciones.Contains(unidad.AsignacionId) &&
                    unidad.Estado == EstadoPublicacionUnidad.Publicada);

            modelo.TareasPublicadas = await contexto.Tareas.CountAsync(tarea =>
                asignaciones.Contains(tarea.AsignacionId) &&
                tarea.Estado == EstadoTarea.Publicada);
        }

        var entregas = contexto.Entregas.AsNoTracking().Where(entrega =>
            entrega.AlumnoId == id &&
            entrega.Estado != EstadoEntrega.Pendiente &&
            !contexto.Entregas.Any(otra =>
                otra.TareaId == entrega.TareaId &&
                otra.AlumnoId == entrega.AlumnoId &&
                otra.NumeroEnvio > entrega.NumeroEnvio));

        if (consultaDocente)
        {
            var docenteId = perfil.DocenteId!.Value;
            entregas = entregas.Where(entrega =>
                entrega.Tarea.Asignacion.DocenteId == docenteId &&
                entrega.Tarea.Asignacion.Estado == EstadoRegistro.Activo &&
                entrega.Tarea.Asignacion.Seccion.CicloEscolar.Activo);
            modelo.Encargados.Clear();
            modelo.Correo = string.Empty;
            modelo.Telefono = null;
            modelo.Usuario = string.Empty;
            modelo.FechaCreacion = default;
        }

        modelo.EntregasRealizadas = await entregas.CountAsync();
        modelo.MostrarContacto = !consultaDocente;
        modelo.MostrarHistorial = !consultaDocente;
        modelo.MostrarEncargados = !consultaDocente;
        modelo.MostrarInformacionAdministrativa = !consultaDocente;

        return ResultadoOperacion<AlumnoDetalle>.Correcto(modelo);
    }

    public async Task PrepararCreacionAsync(CrearAlumno modelo)
    {
        if (!acceso.EsOperadorInstitucional)
        {
            modelo.Secciones = [];
            modelo.Encargados = [];
            return;
        }
        modelo.Secciones = await contexto.Secciones
            .AsNoTracking()
            .Where(seccion =>
                seccion.Estado == EstadoRegistro.Activo &&
                seccion.CicloEscolar.Activo)
            .OrderByDescending(seccion => seccion.CicloEscolar.Anio)
            .ThenBy(seccion => seccion.Grado.Orden)
            .ThenBy(seccion => seccion.Nombre)
            .Select(seccion => new OpcionSeleccion
            {
                Id = seccion.Id,
                Texto = seccion.CicloEscolar.Anio + " - " +
                    seccion.Grado.Nombre + " " + seccion.Nombre
            })
            .ToListAsync();

        modelo.Encargados = await contexto.Encargados
            .AsNoTracking()
            .OrderBy(encargado => encargado.Nombres)
            .ThenBy(encargado => encargado.Apellidos)
            .Select(encargado => new OpcionSeleccion
            {
                Id = encargado.Id,
                Texto = encargado.Nombres + " " + encargado.Apellidos
            })
            .ToListAsync();
    }

    public async Task<ResultadoOperacion<ResultadoContrasenaTemporal>> CrearAsync(
        CrearAlumno modelo)
    {
        if (!acceso.EsOperadorInstitucional)
        {
            return ResultadoOperacion<ResultadoContrasenaTemporal>.Error(
                "No tiene autorización administrativa para crear alumnos.");
        }

        modelo.CodigoPersonal = Limpiar(modelo.CodigoPersonal);

        var seccion = await contexto.Secciones
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.Id == modelo.SeccionId &&
                item.Estado == EstadoRegistro.Activo &&
                item.CicloEscolar.Activo);

        if (seccion == null)
        {
            return ResultadoOperacion<ResultadoContrasenaTemporal>.Validacion(
                nameof(modelo.SeccionId),
                "La sección seleccionada no existe, está inactiva o no pertenece al ciclo escolar activo."
            );
        }

        if (!string.IsNullOrWhiteSpace(modelo.CodigoPersonal) &&
            await ExisteCodigoPersonalAsync(modelo.CodigoPersonal))
        {
            return ResultadoOperacion<ResultadoContrasenaTemporal>.Validacion(
                nameof(modelo.CodigoPersonal),
                "Ya existe un alumno con ese código personal."
            );
        }

        if (modelo.EncargadoId.HasValue != modelo.Parentesco.HasValue)
        {
            var campo = modelo.EncargadoId.HasValue
                ? nameof(modelo.Parentesco)
                : nameof(modelo.EncargadoId);

            return ResultadoOperacion<ResultadoContrasenaTemporal>.Validacion(
                campo,
                "Seleccione tanto el encargado como su parentesco."
            );
        }

        if (modelo.EncargadoId.HasValue)
        {
            var existeEncargado = await contexto.Encargados.AnyAsync(item =>
                item.Id == modelo.EncargadoId.Value);

            if (!existeEncargado)
            {
                return ResultadoOperacion<ResultadoContrasenaTemporal>.Validacion(
                    nameof(modelo.EncargadoId),
                    "El encargado seleccionado no existe."
                );
            }
        }

        await using var transaccion =
            await contexto.Database.BeginTransactionAsync();

        try
        {
            var resultadoCuenta = await cuentaAcademica.CrearAsync(
                modelo,
                Roles.Alumno);

            if (!resultadoCuenta.Exitoso || resultadoCuenta.Datos == null)
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

            var alumno = new Alumno
            {
                UsuarioId = cuenta.Usuario.Id,
                CodigoPersonal = modelo.CodigoPersonal
            };

            contexto.Alumnos.Add(alumno);
            await contexto.SaveChangesAsync();

            contexto.Inscripciones.Add(new Inscripcion
            {
                AlumnoId = alumno.Id,
                SeccionId = seccion.Id,
                CicloEscolarId = seccion.CicloEscolarId,
                Fecha = DateOnly.FromDateTime(DateTime.Today),
                Estado = EstadoInscripcion.Activa
            });

            if (modelo.EncargadoId.HasValue && modelo.Parentesco.HasValue)
            {
                contexto.AlumnoEncargados.Add(new AlumnoEncargado
                {
                    AlumnoId = alumno.Id,
                    EncargadoId = modelo.EncargadoId.Value,
                    Parentesco = modelo.Parentesco.Value
                });
            }

            await entregaPendienteServicio.CrearParaTareasDisponiblesAsync(
                alumno.Id,
                seccion.Id);

            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();

            var mensaje =
                "El alumno fue creado e inscrito correctamente y se generó su cuenta de acceso.";

            try
            {
                await cuentaAcademica.EnviarCredencialesAsync(
                    cuenta.Usuario,
                    cuenta.ContrasenaTemporal);
                mensaje += " Las credenciales fueron enviadas por correo.";
            }
            catch
            {
                mensaje += " No fue posible enviar el correo; entregue la contraseña temporal por otro medio.";
            }

            return ResultadoOperacion<ResultadoContrasenaTemporal>.Correcto(
                cuentaAcademica.CrearResultadoCredenciales(
                    cuenta.Usuario,
                    cuenta.ContrasenaTemporal),
                mensaje);
        }
        catch (DbUpdateException excepcion)
            when (excepcion.Entries.Any(entrada => entrada.Entity is Alumno))
        {
            await transaccion.RollbackAsync();

            if (!string.IsNullOrWhiteSpace(modelo.CodigoPersonal) &&
                await ExisteCodigoPersonalAsync(modelo.CodigoPersonal))
            {
                return ResultadoOperacion<ResultadoContrasenaTemporal>.Validacion(
                    nameof(modelo.CodigoPersonal),
                    "Ya existe un alumno con ese código personal."
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

    public async Task<EditarAlumno?> ObtenerParaEditarAsync(int id)
    {
        if (!acceso.EsOperadorInstitucional)
            return null;

        return await contexto.Alumnos
            .AsNoTracking()
            .Where(alumno => alumno.Id == id)
            .Select(alumno => new EditarAlumno
            {
                Id = alumno.Id,
                PrimerNombre = alumno.Usuario.PrimerNombre,
                SegundoNombre = alumno.Usuario.SegundoNombre,
                TercerNombre = alumno.Usuario.TercerNombre,
                PrimerApellido = alumno.Usuario.PrimerApellido,
                SegundoApellido = alumno.Usuario.SegundoApellido,
                Correo = alumno.Usuario.Email ?? string.Empty,
                CodigoPersonal = alumno.CodigoPersonal
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ResultadoOperacion> EditarAsync(EditarAlumno modelo)
    {
        if (!acceso.EsOperadorInstitucional)
            return ResultadoOperacion.Error(
                "No tiene autorización administrativa para editar alumnos.");

        modelo.CodigoPersonal = Limpiar(modelo.CodigoPersonal);

        if (!string.IsNullOrWhiteSpace(modelo.CodigoPersonal) &&
            await ExisteCodigoPersonalAsync(modelo.CodigoPersonal, modelo.Id))
        {
            return ResultadoOperacion.Validacion(
                nameof(modelo.CodigoPersonal),
                "Ya existe un alumno con ese código personal."
            );
        }

        await using var transaccion =
            await contexto.Database.BeginTransactionAsync();

        try
        {
            var alumno = await contexto.Alumnos
                .Include(item => item.Usuario)
                .FirstOrDefaultAsync(item => item.Id == modelo.Id);

            if (alumno == null)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error(
                    "El alumno seleccionado no existe."
                );
            }

            var resultadoCuenta = await cuentaAcademica.ActualizarAsync(
                alumno.Usuario,
                modelo);

            if (!resultadoCuenta.Exitoso)
            {
                await transaccion.RollbackAsync();
                return resultadoCuenta;
            }

            alumno.CodigoPersonal = modelo.CodigoPersonal;
            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();

            return ResultadoOperacion.Correcto(
                "El alumno fue actualizado correctamente."
            );
        }
        catch (DbUpdateException excepcion)
            when (excepcion.Entries.Any(entrada => entrada.Entity is Alumno))
        {
            await transaccion.RollbackAsync();

            if (!string.IsNullOrWhiteSpace(modelo.CodigoPersonal) &&
                await ExisteCodigoPersonalAsync(modelo.CodigoPersonal, modelo.Id))
            {
                return ResultadoOperacion.Validacion(
                    nameof(modelo.CodigoPersonal),
                    "Ya existe un alumno con ese código personal."
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
            return ResultadoOperacion.Error(
                "No tiene autorización administrativa para cambiar el estado de alumnos.");

        await using var transaccion =
            await contexto.Database.BeginTransactionAsync();

        try
        {
            var alumno = await contexto.Alumnos
                .Include(item => item.Usuario)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (alumno == null)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error(
                    "El alumno seleccionado no existe."
                );
            }

            if (alumno.Usuario.Activo == activo)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion.Error(
                    activo
                        ? "El alumno ya se encuentra activo."
                        : "El alumno ya se encuentra inactivo."
                );
            }

            if (!activo)
            {
                var tieneInscripcion = await contexto.Inscripciones.AnyAsync(
                    inscripcion =>
                        inscripcion.AlumnoId == id &&
                        inscripcion.Estado == EstadoInscripcion.Activa);

                if (tieneInscripcion)
                {
                    await transaccion.RollbackAsync();
                    return ResultadoOperacion.Error(
                        "Finalice o retire la inscripción activa antes de desactivar al alumno."
                    );
                }
            }

            alumno.Usuario.Activo = activo;
            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();

            return ResultadoOperacion.Correcto(
                activo
                    ? "El alumno fue activado correctamente."
                    : "El alumno fue desactivado correctamente."
            );
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    private async Task<bool> PuedeConsultarAlumnoAsync(
        int alumnoId,
        ClaimsPrincipal usuarioActual)
    {
        var usuarioId = administradorUsuarios.GetUserId(usuarioActual);

        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return false;
        }

        if (acceso.EsOperadorInstitucional)
        {
            return true;
        }

        var alumnoActualId = await contexto.Alumnos
            .AsNoTracking()
            .Where(alumno => alumno.UsuarioId == usuarioId)
            .Select(alumno => (int?)alumno.Id)
            .FirstOrDefaultAsync();

        var docenteId = await contexto.Docentes
            .AsNoTracking()
            .Where(docente => docente.UsuarioId == usuarioId)
            .Select(docente => (int?)docente.Id)
            .FirstOrDefaultAsync();

        if (docenteId.HasValue)
        {
            return await contexto.Inscripciones
                .AsNoTracking()
                .AnyAsync(inscripcion =>
                    inscripcion.AlumnoId == alumnoId &&
                    inscripcion.Estado == EstadoInscripcion.Activa &&
                    inscripcion.Seccion.CicloEscolar.Activo &&
                    contexto.Asignaciones.Any(asignacion =>
                        asignacion.DocenteId == docenteId.Value &&
                        asignacion.SeccionId == inscripcion.SeccionId &&
                        asignacion.Estado == EstadoRegistro.Activo &&
                        asignacion.Seccion.CicloEscolar.Activo));
        }

        return alumnoActualId.HasValue && alumnoActualId.Value == alumnoId;
    }

    private Task<bool> ExisteCodigoPersonalAsync(
        string codigoPersonal,
        int? alumnoExcluidoId = null)
    {
        var consulta = contexto.Alumnos
            .AsNoTracking()
            .Where(alumno => alumno.CodigoPersonal == codigoPersonal);

        if (alumnoExcluidoId.HasValue)
        {
            consulta = consulta.Where(alumno =>
                alumno.Id != alumnoExcluidoId.Value);
        }

        return consulta.AnyAsync();
    }

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor)
            ? null
            : valor.Trim();
}

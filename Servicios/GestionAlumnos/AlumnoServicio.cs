using System.ComponentModel.DataAnnotations;
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
using Proyecto_Final.ViewModels.Encargados;

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

    public async Task<ConsultaIndiceAlumnos> ObtenerIndiceAsync()
    {
        var ambito = await ObtenerAmbitoAsync();

        if (ambito.Tipo == TipoAccesoAlumnos.Denegado)
        {
            return new ConsultaIndiceAlumnos
            {
                TipoAcceso = TipoAccesoAlumnos.Denegado
            };
        }

        if (ambito.Tipo == TipoAccesoAlumnos.Institucional)
        {
            return new ConsultaIndiceAlumnos
            {
                TipoAcceso = TipoAccesoAlumnos.Institucional,
                Institucional = await ObtenerIndiceInstitucionalAsync()
            };
        }

        var filas = await AplicarAmbitoContextual(
                contexto.Inscripciones.AsNoTracking(),
                ambito)
            .Select(inscripcion => new FilaAlumnoContextual
            {
                Id = inscripcion.AlumnoId,
                NombreCompleto =
                    inscripcion.Alumno.Usuario.PrimerNombre +
                    (inscripcion.Alumno.Usuario.SegundoNombre == null
                        ? "" : " " + inscripcion.Alumno.Usuario.SegundoNombre) +
                    (inscripcion.Alumno.Usuario.TercerNombre == null
                        ? "" : " " + inscripcion.Alumno.Usuario.TercerNombre) +
                    " " + inscripcion.Alumno.Usuario.PrimerApellido +
                    (inscripcion.Alumno.Usuario.SegundoApellido == null
                        ? "" : " " + inscripcion.Alumno.Usuario.SegundoApellido),
                CicloEscolar = inscripcion.Seccion.CicloEscolar.Anio,
                Grado = inscripcion.Seccion.Grado.Nombre,
                Carrera = inscripcion.Seccion.Grado.Carrera,
                Seccion = inscripcion.Seccion.Nombre
            })
            .ToListAsync();

        var alumnos = filas
            .GroupBy(alumno => alumno.Id)
            .Select(grupo => grupo
                .OrderByDescending(alumno => alumno.CicloEscolar)
                .ThenBy(alumno => alumno.Grado)
                .ThenBy(alumno => alumno.Seccion)
                .First())
            .OrderBy(alumno => alumno.NombreCompleto)
            .Select(alumno => new AlumnoContextual
            {
                NombreCompleto = alumno.NombreCompleto,
                CicloEscolar = alumno.CicloEscolar,
                Grado = alumno.Grado,
                Carrera = alumno.Carrera,
                Seccion = alumno.Seccion
            })
            .ToList();

        return new ConsultaIndiceAlumnos
        {
            TipoAcceso = ambito.Tipo,
            Contextual = new AlumnosContextualesIndice
            {
                TipoAcceso = ambito.Tipo,
                Alumnos = alumnos
            }
        };
    }

    public async Task<ConsultaDetalleAlumno> ObtenerDetalleAutorizadoAsync(
        int id)
    {
        var ambito = await ObtenerAmbitoAsync();

        if (ambito.Tipo == TipoAccesoAlumnos.Denegado)
        {
            return new ConsultaDetalleAlumno
            {
                TipoAcceso = TipoAccesoAlumnos.Denegado
            };
        }

        if (ambito.Tipo == TipoAccesoAlumnos.Institucional)
        {
            return new ConsultaDetalleAlumno
            {
                TipoAcceso = TipoAccesoAlumnos.Institucional,
                Institucional = await ObtenerDetalleInstitucionalAsync(id)
            };
        }

        var alumno = await AplicarAmbitoContextual(
                contexto.Inscripciones.AsNoTracking(),
                ambito)
            .Where(inscripcion => inscripcion.AlumnoId == id)
            .Select(inscripcion => new AlumnoContextual
            {
                NombreCompleto =
                    inscripcion.Alumno.Usuario.PrimerNombre +
                    (inscripcion.Alumno.Usuario.SegundoNombre == null
                        ? "" : " " + inscripcion.Alumno.Usuario.SegundoNombre) +
                    (inscripcion.Alumno.Usuario.TercerNombre == null
                        ? "" : " " + inscripcion.Alumno.Usuario.TercerNombre) +
                    " " + inscripcion.Alumno.Usuario.PrimerApellido +
                    (inscripcion.Alumno.Usuario.SegundoApellido == null
                        ? "" : " " + inscripcion.Alumno.Usuario.SegundoApellido),
                CicloEscolar = inscripcion.Seccion.CicloEscolar.Anio,
                Grado = inscripcion.Seccion.Grado.Nombre,
                Carrera = inscripcion.Seccion.Grado.Carrera,
                Seccion = inscripcion.Seccion.Nombre
            })
            .Distinct()
            .FirstOrDefaultAsync();

        return new ConsultaDetalleAlumno
        {
            TipoAcceso = ambito.Tipo,
            Contextual = alumno
        };
    }

    private async Task<AmbitoAlumnos> ObtenerAmbitoAsync()
    {
        if (acceso.EsOperadorInstitucional)
        {
            return new AmbitoAlumnos(TipoAccesoAlumnos.Institucional);
        }

        var perfil = await acceso.ObtenerContextoUsuarioAsync();

        if (perfil.DocenteId.HasValue)
        {
            return new AmbitoAlumnos(
                TipoAccesoAlumnos.Docente,
                DocenteId: perfil.DocenteId.Value);
        }

        if (!perfil.AlumnoId.HasValue)
        {
            return new AmbitoAlumnos(TipoAccesoAlumnos.Denegado);
        }

        var inscripcion = await contexto.Inscripciones
            .AsNoTracking()
            .Where(item =>
                item.AlumnoId == perfil.AlumnoId.Value &&
                item.Alumno.Usuario.Activo &&
                item.Estado == EstadoInscripcion.Activa &&
                item.Seccion.Estado == EstadoRegistro.Activo &&
                item.Seccion.CicloEscolar.Activo &&
                item.Seccion.CicloEscolar.Estado == EstadoCicloEscolar.Activo)
            .Select(item => new
            {
                item.SeccionId,
                item.CicloEscolarId
            })
            .FirstOrDefaultAsync();

        return new AmbitoAlumnos(
            TipoAccesoAlumnos.Alumno,
            AlumnoId: perfil.AlumnoId.Value,
            SeccionId: inscripcion?.SeccionId,
            CicloEscolarId: inscripcion?.CicloEscolarId);
    }

    private IQueryable<Inscripcion> AplicarAmbitoContextual(
        IQueryable<Inscripcion> consulta,
        AmbitoAlumnos ambito)
    {
        consulta = consulta.Where(inscripcion =>
            inscripcion.Alumno.Usuario.Activo &&
            inscripcion.Estado == EstadoInscripcion.Activa &&
            inscripcion.Seccion.Estado == EstadoRegistro.Activo &&
            inscripcion.Seccion.CicloEscolar.Activo &&
            inscripcion.Seccion.CicloEscolar.Estado == EstadoCicloEscolar.Activo);

        if (ambito.Tipo == TipoAccesoAlumnos.Docente &&
            ambito.DocenteId.HasValue)
        {
            var docenteId = ambito.DocenteId.Value;
            return consulta.Where(inscripcion =>
                contexto.Asignaciones.Any(asignacion =>
                    asignacion.DocenteId == docenteId &&
                    asignacion.SeccionId == inscripcion.SeccionId &&
                    asignacion.Estado == EstadoRegistro.Activo &&
                    asignacion.Curso.Estado == EstadoRegistro.Activo &&
                    asignacion.Seccion.Estado == EstadoRegistro.Activo &&
                    asignacion.Seccion.CicloEscolar.Activo &&
                    asignacion.Seccion.CicloEscolar.Estado ==
                        EstadoCicloEscolar.Activo));
        }

        if (ambito.Tipo == TipoAccesoAlumnos.Alumno &&
            ambito.AlumnoId.HasValue &&
            ambito.SeccionId.HasValue &&
            ambito.CicloEscolarId.HasValue)
        {
            return consulta.Where(inscripcion =>
                inscripcion.AlumnoId != ambito.AlumnoId.Value &&
                inscripcion.SeccionId == ambito.SeccionId.Value &&
                inscripcion.CicloEscolarId == ambito.CicloEscolarId.Value);
        }

        return consulta.Where(_ => false);
    }

    private sealed record AmbitoAlumnos(
        TipoAccesoAlumnos Tipo,
        int? DocenteId = null,
        int? AlumnoId = null,
        int? SeccionId = null,
        int? CicloEscolarId = null);

    private sealed class FilaAlumnoContextual
    {
        public int Id { get; init; }
        public string NombreCompleto { get; init; } = string.Empty;
        public int CicloEscolar { get; init; }
        public string Grado { get; init; } = string.Empty;
        public string Carrera { get; init; } = string.Empty;
        public string Seccion { get; init; } = string.Empty;
    }

    private async Task<AlumnosIndice> ObtenerIndiceInstitucionalAsync()
    {
        var consulta = contexto.Alumnos
            .AsNoTracking()
            .AsQueryable();

        var alumnos = await consulta
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

        return new AlumnosIndice
        {
            Alumnos = alumnos,
            AlertasOcupacion = await ObtenerAlertasOcupacionAsync(null)
        };
    }

    private async Task<AlumnoDetalle?> ObtenerDetalleInstitucionalAsync(int id)
    {
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
            return null;
        }

        var consultaInscripciones = contexto.Inscripciones
            .AsNoTracking()
            .Where(inscripcion => inscripcion.AlumnoId == id);

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

        modelo.EntregasRealizadas = await entregas.CountAsync();
        return modelo;
    }

    public async Task PrepararCreacionAsync(CrearAlumno modelo)
    {
        if (!acceso.EsOperadorInstitucional)
        {
            modelo.Secciones = [];
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

        if (modelo.Secciones.Count == 0)
        {
            modelo.CiclosInactivosConSecciones = await contexto
                .CiclosEscolares
                .AsNoTracking()
                .Where(ciclo =>
                    !ciclo.Activo &&
                    ciclo.Secciones.Any(seccion =>
                        seccion.Estado == EstadoRegistro.Activo))
                .OrderByDescending(ciclo => ciclo.Anio)
                .Select(ciclo => ciclo.Anio)
                .ToListAsync();
        }

    }

    private async Task<List<AlertaOcupacionSeccion>>
        ObtenerAlertasOcupacionAsync(int? docenteId)
    {
        if (!acceso.EsOperadorInstitucional && !docenteId.HasValue)
        {
            return [];
        }

        var inscripciones = contexto.Inscripciones
            .AsNoTracking()
            .Where(inscripcion =>
                inscripcion.Estado == EstadoInscripcion.Activa &&
                inscripcion.Seccion.Estado == EstadoRegistro.Activo &&
                inscripcion.Seccion.CicloEscolar.Activo);

        if (!acceso.EsOperadorInstitucional)
        {
            var idDocente = docenteId!.Value;

            inscripciones = inscripciones.Where(inscripcion =>
                contexto.Asignaciones.Any(asignacion =>
                    asignacion.DocenteId == idDocente &&
                    asignacion.SeccionId == inscripcion.SeccionId &&
                    asignacion.Estado == EstadoRegistro.Activo &&
                    asignacion.Seccion.CicloEscolar.Activo));
        }

        return await inscripciones
            .GroupBy(inscripcion => new
            {
                inscripcion.SeccionId,
                inscripcion.CicloEscolarId,
                Grado = inscripcion.Seccion.Grado.Nombre,
                Carrera = inscripcion.Seccion.Grado.Carrera,
                Seccion = inscripcion.Seccion.Nombre,
                Ciclo = inscripcion.Seccion.CicloEscolar.Anio
            })
            .Select(grupo => new AlertaOcupacionSeccion
            {
                Grado = grupo.Key.Grado,
                Carrera = grupo.Key.Carrera,
                Seccion = grupo.Key.Seccion,
                Ciclo = grupo.Key.Ciclo,
                CantidadAlumnos = grupo
                    .Select(inscripcion => inscripcion.AlumnoId)
                    .Distinct()
                    .Count()
            })
            .Where(alerta => alerta.CantidadAlumnos >= 20)
            .OrderByDescending(alerta => alerta.CantidadAlumnos)
            .ThenByDescending(alerta => alerta.Ciclo)
            .ThenBy(alerta => alerta.Grado)
            .ThenBy(alerta => alerta.Carrera)
            .ThenBy(alerta => alerta.Seccion)
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

        var validacionEncargado = await ValidarNuevoEncargadoAsync(modelo);
        if (validacionEncargado != null) return validacionEncargado;

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

            if (modelo.RegistrarEncargadoAhora)
            {
                var datos = modelo.DatosEncargado;
                var encargado = new Encargado
                {
                    Nombres = datos.Nombres.Trim(), Apellidos = datos.Apellidos.Trim(),
                    Dpi = Limpiar(datos.Dpi), Telefono = datos.Telefono.Trim(),
                    TelefonoAlterno = Limpiar(datos.TelefonoAlterno),
                    Correo = Limpiar(datos.Correo)?.ToLowerInvariant(),
                    Direccion = Limpiar(datos.Direccion), FechaCreacion = DateTime.UtcNow
                };
                contexto.Encargados.Add(encargado);
                await contexto.SaveChangesAsync();
                contexto.AlumnoEncargados.Add(new AlumnoEncargado
                {
                    AlumnoId = alumno.Id,
                    EncargadoId = encargado.Id,
                    Parentesco = modelo.ParentescoEncargado!.Value
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

        var modelo = await contexto.Alumnos
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

        if (modelo != null) await PrepararEdicionAsync(modelo);
        return modelo;
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

        var validacionEncargados = await ValidarEncargadosAsync(modelo);
        if (validacionEncargados != null) return validacionEncargados;

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
            var relaciones = await contexto.AlumnoEncargados
                .Where(x => x.AlumnoId == alumno.Id).ToListAsync();
            var recibidas = modelo.Encargados.ToDictionary(x => x.EncargadoId);

            contexto.AlumnoEncargados.RemoveRange(relaciones.Where(x =>
                !recibidas.ContainsKey(x.EncargadoId)));

            foreach (var relacion in relaciones.Where(x =>
                recibidas.ContainsKey(x.EncargadoId)))
            {
                relacion.Parentesco = recibidas[relacion.EncargadoId].Parentesco;
            }

            foreach (var nueva in recibidas.Values.Where(x =>
                relaciones.All(actual => actual.EncargadoId != x.EncargadoId)))
            {
                contexto.AlumnoEncargados.Add(new AlumnoEncargado
                {
                    AlumnoId = alumno.Id,
                    EncargadoId = nueva.EncargadoId,
                    Parentesco = nueva.Parentesco
                });
            }
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

    public async Task PrepararEdicionAsync(EditarAlumno modelo)
    {
        modelo.Encargados = await contexto.AlumnoEncargados.AsNoTracking()
            .Where(x => x.AlumnoId == modelo.Id)
            .OrderBy(x => x.Encargado.Nombres).ThenBy(x => x.Encargado.Apellidos)
            .Select(x => new EncargadoAlumnoEdicion
            {
                EncargadoId = x.EncargadoId,
                NombreCompleto = x.Encargado.Nombres + " " + x.Encargado.Apellidos,
                Parentesco = x.Parentesco
            }).ToListAsync();
        await PrepararOpcionesEncargadosAsync(modelo);
    }

    public async Task PrepararOpcionesEncargadosAsync(EditarAlumno modelo)
    {
        modelo.OpcionesEncargados = await contexto.Encargados.AsNoTracking()
            .OrderBy(x => x.Nombres).ThenBy(x => x.Apellidos)
            .Select(x => new OpcionSeleccion { Id = x.Id, Texto = x.Nombres + " " + x.Apellidos + (x.Dpi == null ? "" : " · " + x.Dpi) })
            .ToListAsync();
    }

    private async Task<ResultadoOperacion<ResultadoContrasenaTemporal>?> ValidarNuevoEncargadoAsync(CrearAlumno modelo)
    {
        if (!modelo.RegistrarEncargadoAhora) return null;
        if (!modelo.ParentescoEncargado.HasValue || !Enum.IsDefined(modelo.ParentescoEncargado.Value))
            return ResultadoOperacion<ResultadoContrasenaTemporal>.Validacion(nameof(modelo.ParentescoEncargado), "Seleccione el parentesco del encargado.");
        var resultados = new List<ValidationResult>();
        if (!Validator.TryValidateObject(modelo.DatosEncargado, new ValidationContext(modelo.DatosEncargado), resultados, true))
            return ResultadoOperacion<ResultadoContrasenaTemporal>.Validacion(resultados.ToDictionary(x => "DatosEncargado." + (x.MemberNames.FirstOrDefault() ?? string.Empty), x => x.ErrorMessage ?? "Datos inválidos."));
        var dpi = Limpiar(modelo.DatosEncargado.Dpi);
        if (dpi != null && await contexto.Encargados.AnyAsync(x => x.Dpi == dpi))
            return ResultadoOperacion<ResultadoContrasenaTemporal>.Validacion("DatosEncargado.Dpi", "Ya existe un encargado con ese DPI. Puede vincularlo después desde Editar alumno.");
        return null;
    }

    private async Task<ResultadoOperacion?> ValidarEncargadosAsync(EditarAlumno modelo)
    {
        if (modelo.NuevoEncargadoId.HasValue || modelo.NuevoParentesco.HasValue)
        {
            if (!modelo.NuevoEncargadoId.HasValue || !modelo.NuevoParentesco.HasValue)
                return ResultadoOperacion.Validacion(string.Empty, "Seleccione tanto el encargado como su parentesco.");
            modelo.Encargados.Add(new EncargadoAlumnoEdicion { EncargadoId = modelo.NuevoEncargadoId.Value, Parentesco = modelo.NuevoParentesco.Value });
        }
        if (modelo.Encargados.GroupBy(x => x.EncargadoId).Any(x => x.Count() > 1))
            return ResultadoOperacion.Validacion(string.Empty, "No puede vincular el mismo encargado más de una vez.");
        if (modelo.Encargados.Any(x => x.EncargadoId <= 0 || !Enum.IsDefined(x.Parentesco)))
            return ResultadoOperacion.Validacion(string.Empty, "Los datos de los encargados no son válidos.");
        var ids = modelo.Encargados.Select(x => x.EncargadoId).ToList();
        if (ids.Count != await contexto.Encargados.CountAsync(x => ids.Contains(x.Id)))
            return ResultadoOperacion.Validacion(string.Empty, "Uno de los encargados seleccionados no existe.");
        return null;
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

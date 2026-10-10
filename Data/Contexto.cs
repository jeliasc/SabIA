using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Models;

namespace Proyecto_Final.Data;

public class Contexto
    : IdentityDbContext<Usuario, Rol, string>
{
    public Contexto(
        DbContextOptions<Contexto> options)
        : base(options)
    {
    }
    public DbSet<PermisoSistema> PermisosSistema =>
        Set<PermisoSistema>();
    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();

    public DbSet<Docente> Docentes =>
        Set<Docente>();

    public DbSet<Alumno> Alumnos => Set<Alumno>();

    public DbSet<Encargado> Encargados => Set<Encargado>();

    public DbSet<AlumnoEncargado> AlumnoEncargados =>
        Set<AlumnoEncargado>();

    public DbSet<CicloEscolar> CiclosEscolares =>
        Set<CicloEscolar>();

    public DbSet<MovimientoCicloEscolar> MovimientosCiclosEscolares =>
        Set<MovimientoCicloEscolar>();

    public DbSet<Periodo> Periodos =>
        Set<Periodo>();

    public DbSet<Grado> Grados =>
        Set<Grado>();

    public DbSet<Seccion> Secciones =>
        Set<Seccion>();

    public DbSet<Curso> Cursos =>
        Set<Curso>();

    public DbSet<Asignacion> Asignaciones =>
        Set<Asignacion>();

    public DbSet<Inscripcion> Inscripciones =>
        Set<Inscripcion>();

    public DbSet<HistorialTraslado> HistorialTraslados =>
        Set<HistorialTraslado>();

    public DbSet<Unidad> Unidades =>
        Set<Unidad>();

    public DbSet<UnidadAsignacion> UnidadesAsignaciones =>
        Set<UnidadAsignacion>();

    public DbSet<Archivo> Archivos =>
        Set<Archivo>();

    public DbSet<Material> Materiales =>
        Set<Material>();

    public DbSet<Planificacion> Planificaciones =>
        Set<Planificacion>();

    public DbSet<PlanificacionDetalle> PlanificacionesDetalles =>
        Set<PlanificacionDetalle>();

    public DbSet<PlanificacionAsignacion> PlanificacionesAsignaciones =>
        Set<PlanificacionAsignacion>();

    public DbSet<PlanificacionArchivo> PlanificacionesArchivos =>
        Set<PlanificacionArchivo>();

    public DbSet<RevisionPlanificacion> RevisionesPlanificaciones =>
        Set<RevisionPlanificacion>();

    public DbSet<Tarea> Tareas => Set<Tarea>();

    public DbSet<TareaArchivo> TareasArchivos =>
        Set<TareaArchivo>();

    public DbSet<Entrega> Entregas => Set<Entrega>();

    public DbSet<ReaperturaEntrega> ReaperturasEntregas =>
        Set<ReaperturaEntrega>();

    public DbSet<EntregaArchivo> EntregasArchivos =>
        Set<EntregaArchivo>();

    public DbSet<Cuestionario> Cuestionarios =>
        Set<Cuestionario>();

    public DbSet<Pregunta> Preguntas => Set<Pregunta>();

    public DbSet<OpcionPregunta> OpcionesPreguntas =>
        Set<OpcionPregunta>();

    public DbSet<RespuestaAceptada> RespuestasAceptadas =>
        Set<RespuestaAceptada>();

    public DbSet<IntentoCuestionario> IntentosCuestionarios =>
        Set<IntentoCuestionario>();

    public DbSet<RespuestaAlumno> RespuestasAlumnos =>
        Set<RespuestaAlumno>();

    public DbSet<RespuestaAlumnoOpcion> RespuestasAlumnosOpciones =>
        Set<RespuestaAlumnoOpcion>();
    // Calificaciones y seguimiento académico

    public DbSet<ConfiguracionEvaluacion> ConfiguracionesEvaluacion =>
        Set<ConfiguracionEvaluacion>();

    public DbSet<CategoriaEvaluacion> CategoriasEvaluacion =>
        Set<CategoriaEvaluacion>();

    public DbSet<ActividadEvaluable> ActividadesEvaluables =>
        Set<ActividadEvaluable>();

    public DbSet<CalificacionManual> CalificacionesManuales =>
        Set<CalificacionManual>();

    public DbSet<ConfiguracionNotaAnual> ConfiguracionesNotasAnuales =>
        Set<ConfiguracionNotaAnual>();

    public DbSet<PonderacionPeriodoAnual> PonderacionesPeriodosAnuales =>
        Set<PonderacionPeriodoAnual>();

    public DbSet<CierreCalificaciones> CierresCalificaciones =>
        Set<CierreCalificaciones>();

    public DbSet<MovimientoCierreCalificaciones> MovimientosCierresCalificaciones =>
        Set<MovimientoCierreCalificaciones>();

    public DbSet<HistorialCalificacion> HistorialesCalificaciones =>
        Set<HistorialCalificacion>();

    public DbSet<PlantillaEvaluacion> PlantillasEvaluacion =>
        Set<PlantillaEvaluacion>();

    public DbSet<ResultadoCalificacionPeriodo> ResultadosCalificacionesPeriodos =>
        Set<ResultadoCalificacionPeriodo>();

    public DbSet<SolicitudCorreccionCalificacion> SolicitudesCorreccionesCalificaciones =>
        Set<SolicitudCorreccionCalificacion>();

    protected override void OnModelCreating(
    ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<RegistroAuditoria>().HasIndex(x => x.FechaUtc);
        builder.Entity<RegistroAuditoria>().HasIndex(x => new { x.UsuarioId, x.FechaUtc });
        builder.Entity<RegistroAuditoria>().HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CicloEscolar>()
            .HasIndex(x => x.Activo)
            .IsUnique()
            .HasFilter("\"Activo\" = TRUE");
        builder.Entity<CicloEscolar>()
            .ToTable(tabla => tabla.HasCheckConstraint(
                "CK_CiclosEscolares_EstadoActivo",
                "(\"Estado\" = 2 AND \"Activo\" = TRUE) OR (\"Estado\" <> 2 AND \"Activo\" = FALSE)"));

        builder.Entity<MovimientoCicloEscolar>()
            .HasIndex(x => new { x.CicloEscolarId, x.FechaUtc });
        builder.Entity<MovimientoCicloEscolar>()
            .HasOne(x => x.CicloEscolar)
            .WithMany(x => x.Movimientos)
            .HasForeignKey(x => x.CicloEscolarId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<MovimientoCicloEscolar>()
            .HasOne(x => x.RealizadoPorUsuario)
            .WithMany()
            .HasForeignKey(x => x.RealizadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PermisoSistema>()
            .HasIndex(permiso => permiso.Codigo)
            .IsUnique();

        builder.Entity<Docente>()
            .HasIndex(docente => docente.UsuarioId)
            .IsUnique();

        builder.Entity<Docente>()
            .HasIndex(docente => docente.Carnet)
            .IsUnique();

        builder.Entity<Docente>()
            .HasOne(docente => docente.Usuario)
            .WithOne()
            .HasForeignKey<Docente>(docente => docente.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Alumno - Usuario (1:1)
        builder.Entity<Alumno>()
            .HasIndex(alumno => alumno.UsuarioId)
            .IsUnique();

        builder.Entity<Alumno>()
            .HasIndex(alumno => alumno.CodigoPersonal)
            .IsUnique();

        builder.Entity<Alumno>()
            .HasOne(alumno => alumno.Usuario)
            .WithOne()
            .HasForeignKey<Alumno>(alumno => alumno.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Encargado
        builder.Entity<Encargado>()
            .HasIndex(encargado => encargado.Dpi)
            .IsUnique();

        // Alumno - Encargado (N:M)
        builder.Entity<AlumnoEncargado>()
            .HasKey(relacion => new
            {
                relacion.AlumnoId,
                relacion.EncargadoId
            });

        builder.Entity<AlumnoEncargado>()
            .HasOne(relacion => relacion.Alumno)
            .WithMany(alumno => alumno.AlumnoEncargados)
            .HasForeignKey(relacion => relacion.AlumnoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<AlumnoEncargado>()
            .HasOne(relacion => relacion.Encargado)
            .WithMany(encargado => encargado.AlumnoEncargados)
            .HasForeignKey(relacion => relacion.EncargadoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<AlumnoEncargado>()
           .HasIndex(relacion => relacion.EncargadoId);

        // CICLOS ESCOLARES

        builder.Entity<CicloEscolar>()
            .HasIndex(ciclo => ciclo.Anio)
            .IsUnique();

        builder.Entity<CicloEscolar>()
            .ToTable(tabla => tabla.HasCheckConstraint(
                "CK_CiclosEscolares_Fechas",
                "\"FechaInicio\" <= \"FechaFin\""));

        // PERIODOS

        builder.Entity<Periodo>()
            .HasAlternateKey(p => new
            {
                p.Id,
                p.CicloEscolarId
            });

        builder.Entity<Periodo>()
            .HasIndex(periodo => new
            {
                periodo.CicloEscolarId,
                periodo.Numero
            })
            .IsUnique();

        builder.Entity<Periodo>()
            .HasOne(periodo => periodo.CicloEscolar)
            .WithMany(ciclo => ciclo.Periodos)
            .HasForeignKey(periodo => periodo.CicloEscolarId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Periodo>()
            .ToTable(tabla => tabla.HasCheckConstraint(
                "CK_Periodos_Fechas",
                "\"FechaInicio\" <= \"FechaFin\""));

        // GRADOS

        builder.Entity<Grado>()
            .HasIndex(grado => new
            {
                grado.Nivel,
                grado.Nombre,
                grado.Carrera
            })
            .IsUnique();

        // SECCIONES

        builder.Entity<Seccion>()
            .HasIndex(seccion => new
            {
                seccion.CicloEscolarId,
                seccion.GradoId,
                seccion.Nombre
            })
            .IsUnique();

        builder.Entity<Seccion>()
            .HasOne(seccion => seccion.CicloEscolar)
            .WithMany(ciclo => ciclo.Secciones)
            .HasForeignKey(seccion => seccion.CicloEscolarId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Seccion>()
            .HasOne(seccion => seccion.Grado)
            .WithMany(grado => grado.Secciones)
            .HasForeignKey(seccion => seccion.GradoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Seccion>()
            .HasIndex(seccion => seccion.GradoId);

        // CURSOS

        builder.Entity<Curso>()
            .HasIndex(curso => new
            {
                curso.GradoId,
                curso.Nombre
            })
            .IsUnique();

        builder.Entity<Curso>()
            .HasOne(curso => curso.Grado)
            .WithMany(grado => grado.Cursos)
            .HasForeignKey(curso => curso.GradoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Asignaciones

        builder.Entity<Asignacion>()
    .HasAlternateKey(a => new
    {
        a.Id,
        a.SeccionId
    });

        builder.Entity<Asignacion>()
            .HasIndex(asignacion => new
            {
                asignacion.SeccionId,
                asignacion.CursoId
            })
            .IsUnique();

        builder.Entity<Asignacion>()
            .HasOne(asignacion => asignacion.Seccion)
            .WithMany()
            .HasForeignKey(asignacion => new
            {
                asignacion.SeccionId,
                asignacion.GradoId
            })
            .HasPrincipalKey(seccion => new
            {
                seccion.Id,
                seccion.GradoId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Asignacion>()
            .HasOne(asignacion => asignacion.Curso)
            .WithMany()
            .HasForeignKey(asignacion => new
            {
                asignacion.CursoId,
                asignacion.GradoId
            })
            .HasPrincipalKey(curso => new
            {
                curso.Id,
                curso.GradoId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Asignacion>()
            .HasOne(asignacion => asignacion.Docente)
            .WithMany()
            .HasForeignKey(asignacion => asignacion.DocenteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Asignacion>()
            .HasIndex(asignacion => asignacion.DocenteId);

        // Inscripciones

        builder.Entity<Inscripcion>()
            .HasIndex(inscripcion => new
            {
                inscripcion.AlumnoId,
                inscripcion.CicloEscolarId
            })
            .HasDatabaseName(
                "IX_Inscripciones_AlumnoId_CicloEscolarId_Activa")
            .HasFilter("\"Estado\" = 1")
            .IsUnique();

        builder.Entity<Inscripcion>()
            .HasIndex(inscripcion => new
            {
                inscripcion.AlumnoId,
                inscripcion.CicloEscolarId,
                inscripcion.Estado
            });

        builder.Entity<Inscripcion>()
            .HasAlternateKey(inscripcion => new
            {
                inscripcion.Id,
                inscripcion.AlumnoId
            });

        builder.Entity<Inscripcion>()
            .HasOne(inscripcion => inscripcion.Alumno)
            .WithMany()
            .HasForeignKey(inscripcion => inscripcion.AlumnoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Inscripcion>()
            .HasOne(inscripcion => inscripcion.Seccion)
            .WithMany()
            .HasForeignKey(inscripcion => new
            {
                inscripcion.SeccionId,
                inscripcion.CicloEscolarId
            })
            .HasPrincipalKey(seccion => new
            {
                seccion.Id,
                seccion.CicloEscolarId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Inscripcion>()
            .HasIndex(inscripcion => inscripcion.SeccionId);

        // Historial de traslados

        builder.Entity<HistorialTraslado>()
            .HasOne(historial => historial.Inscripcion)
            .WithMany()
            .HasForeignKey(historial => new
            {
                historial.InscripcionId,
                historial.CicloEscolarId
            })
            .HasPrincipalKey(inscripcion => new
            {
                inscripcion.Id,
                inscripcion.CicloEscolarId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<HistorialTraslado>()
            .HasOne(historial => historial.SeccionOrigen)
            .WithMany()
            .HasForeignKey(historial => new
            {
                historial.SeccionOrigenId,
                historial.CicloEscolarId
            })
            .HasPrincipalKey(seccion => new
            {
                seccion.Id,
                seccion.CicloEscolarId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<HistorialTraslado>()
            .HasOne(historial => historial.SeccionDestino)
            .WithMany()
            .HasForeignKey(historial => new
            {
                historial.SeccionDestinoId,
                historial.CicloEscolarId
            })
            .HasPrincipalKey(seccion => new
            {
                seccion.Id,
                seccion.CicloEscolarId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<HistorialTraslado>()
            .HasOne(historial => historial.RealizadoPorUsuario)
            .WithMany()
            .HasForeignKey(historial => historial.RealizadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<HistorialTraslado>()
            .HasIndex(historial => new
            {
                historial.InscripcionId,
                historial.FechaTraslado
            });


        // Unidades

        builder.Entity<Unidad>()
            .HasOne(unidad => unidad.UnidadOrigen)
            .WithMany()
            .HasForeignKey(unidad => unidad.UnidadOrigenId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Unidad>()
            .HasOne(unidad => unidad.CreadoPorUsuario)
            .WithMany()
            .HasForeignKey(unidad => unidad.CreadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Unidad>()
            .HasIndex(unidad => unidad.CreadoPorUsuarioId);

        // Unidades asignadas

        builder.Entity<UnidadAsignacion>()
            .HasIndex(registro => new
            {
                registro.AsignacionId,
                registro.UnidadId
            })
            .IsUnique();

        builder.Entity<UnidadAsignacion>()
            .HasIndex(registro => new
            {
                registro.AsignacionId,
                registro.Orden
            })
            .IsUnique();

        builder.Entity<UnidadAsignacion>()
            .HasOne(registro => registro.Unidad)
            .WithMany(unidad => unidad.UnidadAsignaciones)
            .HasForeignKey(registro => registro.UnidadId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UnidadAsignacion>()
            .HasOne(registro => registro.Asignacion)
            .WithMany()
            .HasForeignKey(registro => registro.AsignacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UnidadAsignacion>()
            .HasOne(registro => registro.Periodo)
            .WithMany()
            .HasForeignKey(registro => registro.PeriodoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UnidadAsignacion>()
            .HasOne(registro => registro.AsignadoPorUsuario)
            .WithMany()
            .HasForeignKey(registro => registro.AsignadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Archivos

        builder.Entity<Archivo>()
            .HasIndex(archivo => archivo.ClaveAlmacenamiento)
            .IsUnique();

        builder.Entity<Archivo>()
            .HasOne(archivo => archivo.SubidoPorUsuario)
            .WithMany()
            .HasForeignKey(archivo => archivo.SubidoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Materiales

        builder.Entity<Material>()
            .HasIndex(material => new
            {
                material.UnidadId,
                material.Orden
            })
            .IsUnique();

        builder.Entity<Material>()
            .HasOne(material => material.Unidad)
            .WithMany(unidad => unidad.Materiales)
            .HasForeignKey(material => material.UnidadId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Material>()
            .HasOne(material => material.Archivo)
            .WithMany()
            .HasForeignKey(material => material.ArchivoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Material>()
            .ToTable(tabla => tabla.HasCheckConstraint(
                "CK_Materiales_Fuente",
                "(\"ArchivoId\" IS NOT NULL AND \"UrlExterna\" IS NULL) OR " +
                "(\"ArchivoId\" IS NULL AND \"UrlExterna\" IS NOT NULL " +
                "AND LENGTH(TRIM(\"UrlExterna\")) > 0)"));


        // Planificaciones

        builder.Entity<Planificacion>()
            .HasIndex(p => new
            {
                p.GrupoVersionId,
                p.NumeroVersion
            })
            .IsUnique();

        builder.Entity<Planificacion>()
            .HasIndex(p => new
            {
                p.Estado,
                p.FechaEnvioRevision
            });

        builder.Entity<Planificacion>()
            .HasIndex(p => new
            {
                p.DocenteId,
                p.Estado
            });

        builder.Entity<Planificacion>()
            .HasOne(p => p.Docente)
            .WithMany()
            .HasForeignKey(p => p.DocenteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Planificacion>()
            .HasOne(p => p.PlanificacionAnterior)
            .WithMany()
            .HasForeignKey(p => p.PlanificacionAnteriorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Planificacion>()
            .HasOne(p => p.AprobadoPorUsuario)
            .WithMany()
            .HasForeignKey(p => p.AprobadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Detalles

        builder.Entity<PlanificacionDetalle>()
            .HasIndex(d => new
            {
                d.PlanificacionId,
                d.Orden
            })
            .IsUnique();

        builder.Entity<PlanificacionDetalle>()
            .HasOne(d => d.Planificacion)
            .WithMany(p => p.Detalles)
            .HasForeignKey(d => d.PlanificacionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Asignaciones

        builder.Entity<PlanificacionAsignacion>()
            .HasIndex(a => new
            {
                a.PlanificacionId,
                a.AsignacionId
            })
            .IsUnique();

        builder.Entity<PlanificacionAsignacion>()
            .HasOne(a => a.Planificacion)
            .WithMany(p => p.Asignaciones)
            .HasForeignKey(a => a.PlanificacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PlanificacionAsignacion>()
            .HasOne(a => a.Asignacion)
            .WithMany()
            .HasForeignKey(a => a.AsignacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PlanificacionAsignacion>()
            .HasOne(a => a.Periodo)
            .WithMany()
            .HasForeignKey(a => a.PeriodoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PlanificacionAsignacion>()
            .HasOne(a => a.UnidadAsignacion)
            .WithMany()
            .HasForeignKey(a => a.UnidadAsignacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PlanificacionAsignacion>()
            .HasIndex(a => a.AsignacionId);

        // Archivos

        builder.Entity<PlanificacionArchivo>()
            .HasKey(a => new
            {
                a.PlanificacionId,
                a.ArchivoId
            });

        builder.Entity<PlanificacionArchivo>()
            .HasOne(a => a.Planificacion)
            .WithMany(p => p.Archivos)
            .HasForeignKey(a => a.PlanificacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PlanificacionArchivo>()
            .HasOne(a => a.Archivo)
            .WithMany()
            .HasForeignKey(a => a.ArchivoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Revisiones

        builder.Entity<RevisionPlanificacion>()
            .HasOne(r => r.Planificacion)
            .WithMany(p => p.Revisiones)
            .HasForeignKey(r => r.PlanificacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RevisionPlanificacion>()
            .HasOne(r => r.RealizadoPorUsuario)
            .WithMany()
            .HasForeignKey(r => r.RealizadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RevisionPlanificacion>()
            .HasIndex(r => new
            {
                r.PlanificacionId,
                r.Fecha
            });

        builder.Entity<RevisionPlanificacion>()
            .HasIndex(r => r.OperacionLoteId);

        // Fechas de planificación

        builder.Entity<PlanificacionAsignacion>()
            .ToTable(tabla => tabla.HasCheckConstraint(
                "CK_PlanificacionesAsignaciones_Fechas",
                "\"FechaInicio\" <= \"FechaFin\""));

        // Tareas

        builder.Entity<Tarea>()
             .HasAlternateKey(t => new
             {
                 t.Id,
                 t.AsignacionId
             });

        builder.Entity<Tarea>()
            .HasOne(t => t.Asignacion)
            .WithMany()
            .HasForeignKey(t => t.AsignacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Tarea>()
            .HasOne(t => t.UnidadAsignacion)
            .WithMany()
            .HasForeignKey(t => t.UnidadAsignacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Tarea>()
            .HasOne(t => t.CreadoPorUsuario)
            .WithMany()
            .HasForeignKey(t => t.CreadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Tarea>()
            .HasIndex(t => new
            {
                t.AsignacionId,
                t.Estado,
                t.FechaLimite
            });


        builder.Entity<Tarea>()
            .ToTable(tabla =>
            {
                tabla.HasCheckConstraint(
                    "CK_Tareas_Fechas",
                    "\"FechaDisponibilidad\" IS NULL OR \"FechaLimite\" IS NULL OR " +
                    "\"FechaDisponibilidad\" <= \"FechaLimite\"");

                tabla.HasCheckConstraint(
                    "CK_Tareas_Punteo",
                    "\"PunteoMaximo\" >= 0");

                tabla.HasCheckConstraint(
                    "CK_Tareas_NumeroVersion",
                    "\"NumeroVersion\" >= 1");
            });

        builder.Entity<Tarea>()
            .HasIndex(t => new
            {
                t.GrupoVersionId,
                t.NumeroVersion
            })
            .IsUnique();

        builder.Entity<Tarea>()
            .HasOne(t => t.TareaAnterior)
            .WithMany()
            .HasForeignKey(t => t.TareaAnteriorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Archivos de tareas

        builder.Entity<TareaArchivo>()
            .HasKey(a => new
            {
                a.TareaId,
                a.ArchivoId
            });

        builder.Entity<TareaArchivo>()
            .HasOne(a => a.Tarea)
            .WithMany(t => t.Archivos)
            .HasForeignKey(a => a.TareaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TareaArchivo>()
            .HasOne(a => a.Archivo)
            .WithMany()
            .HasForeignKey(a => a.ArchivoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Entregas

        builder.Entity<Entrega>()
            .HasIndex(e => new
            {
                e.TareaId,
                e.AlumnoId,
                e.NumeroEnvio
            })
            .IsUnique()
            .HasFilter("\"InscripcionId\" IS NULL");

        builder.Entity<Entrega>()
            .HasIndex(e => new
            {
                e.TareaId,
                e.InscripcionId,
                e.NumeroEnvio
            })
            .IsUnique();

        builder.Entity<Entrega>()
            .HasOne(e => e.Tarea)
            .WithMany(t => t.Entregas)
            .HasForeignKey(e => e.TareaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Entrega>()
            .HasOne(e => e.Alumno)
            .WithMany()
            .HasForeignKey(e => e.AlumnoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Entrega>()
            .HasOne(e => e.CalificadoPorUsuario)
            .WithMany()
            .HasForeignKey(e => e.CalificadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Entrega>()
            .HasOne(e => e.Inscripcion)
            .WithMany()
            .HasForeignKey(e => new { e.InscripcionId, e.AlumnoId })
            .HasPrincipalKey(i => new { i.Id, i.AlumnoId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ReaperturaEntrega>()
            .ToTable(tabla => tabla.HasCheckConstraint(
                "CK_ReaperturasEntregas_Fechas",
                "\"FechaLimite\" > \"FechaReapertura\""));

        builder.Entity<ReaperturaEntrega>()
            .HasOne(reapertura => reapertura.Entrega)
            .WithMany(entrega => entrega.Reaperturas)
            .HasForeignKey(reapertura => reapertura.EntregaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ReaperturaEntrega>()
            .HasOne(reapertura => reapertura.ReabiertaPorUsuario)
            .WithMany()
            .HasForeignKey(reapertura => reapertura.ReabiertaPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ReaperturaEntrega>()
            .HasIndex(reapertura => new
            {
                reapertura.EntregaId,
                reapertura.FechaReapertura
            });

        // Archivos de entregas

        builder.Entity<EntregaArchivo>()
            .HasKey(a => new
            {
                a.EntregaId,
                a.ArchivoId
            });

        builder.Entity<EntregaArchivo>()
            .HasOne(a => a.Entrega)
            .WithMany(e => e.Archivos)
            .HasForeignKey(a => a.EntregaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<EntregaArchivo>()
            .HasOne(a => a.Archivo)
            .WithMany()
            .HasForeignKey(a => a.ArchivoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Cuestionarios

        builder.Entity<Cuestionario>()
            .HasIndex(c => c.TareaId)
            .IsUnique();

        builder.Entity<Cuestionario>()
            .HasOne(c => c.Tarea)
            .WithOne(t => t.Cuestionario)
            .HasForeignKey<Cuestionario>(c => c.TareaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Preguntas

        builder.Entity<Pregunta>()
            .HasIndex(p => new
            {
                p.CuestionarioId,
                p.Orden
            })
            .IsUnique();

        builder.Entity<Pregunta>()
            .HasOne(p => p.Cuestionario)
            .WithMany(c => c.Preguntas)
            .HasForeignKey(p => p.CuestionarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Opciones de preguntas

        builder.Entity<OpcionPregunta>()
            .HasIndex(o => new
            {
                o.PreguntaId,
                o.Orden
            })
            .IsUnique();

        builder.Entity<OpcionPregunta>()
            .HasOne(o => o.Pregunta)
            .WithMany(p => p.Opciones)
            .HasForeignKey(o => o.PreguntaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OpcionPregunta>()
            .HasAlternateKey(o => new
            {
                o.Id,
                o.PreguntaId
            });

        // Respuestas aceptadas

        builder.Entity<RespuestaAceptada>()
            .HasOne(r => r.Pregunta)
            .WithMany(p => p.RespuestasAceptadas)
            .HasForeignKey(r => r.PreguntaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Intentos

        builder.Entity<IntentoCuestionario>()
            .HasIndex(i => new
            {
                i.CuestionarioId,
                i.AlumnoId,
                i.NumeroIntento
            })
            .IsUnique()
            .HasFilter("\"InscripcionId\" IS NULL");

        builder.Entity<IntentoCuestionario>()
            .HasIndex(i => new
            {
                i.CuestionarioId,
                i.InscripcionId,
                i.NumeroIntento
            })
            .IsUnique();

        builder.Entity<IntentoCuestionario>()
            .HasOne(i => i.Cuestionario)
            .WithMany(c => c.Intentos)
            .HasForeignKey(i => i.CuestionarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IntentoCuestionario>()
            .HasOne(i => i.Alumno)
            .WithMany()
            .HasForeignKey(i => i.AlumnoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<IntentoCuestionario>()
            .HasOne(i => i.Inscripcion)
            .WithMany()
            .HasForeignKey(i => new { i.InscripcionId, i.AlumnoId })
            .HasPrincipalKey(i => new { i.Id, i.AlumnoId })
            .OnDelete(DeleteBehavior.Restrict);

        // Respuestas de alumnos
        builder.Entity<RespuestaAlumno>()
            .HasAlternateKey(r => new
            {
                r.Id,
                r.PreguntaId
            });
        builder.Entity<RespuestaAlumno>()
            .HasIndex(r => new
            {
                r.IntentoCuestionarioId,
                r.PreguntaId
            })
            .IsUnique();

        builder.Entity<RespuestaAlumno>()
            .HasOne(r => r.IntentoCuestionario)
            .WithMany(i => i.Respuestas)
            .HasForeignKey(r => r.IntentoCuestionarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RespuestaAlumno>()
            .HasOne(r => r.Pregunta)
            .WithMany()
            .HasForeignKey(r => r.PreguntaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RespuestaAlumno>()
            .HasOne(r => r.CalificadoPorUsuario)
            .WithMany()
            .HasForeignKey(r => r.CalificadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Opciones seleccionadas

        builder.Entity<RespuestaAlumnoOpcion>()
            .HasKey(r => new
            {
                r.RespuestaAlumnoId,
                r.OpcionPreguntaId
            });

        builder.Entity<RespuestaAlumnoOpcion>()
            .HasOne(r => r.RespuestaAlumno)
            .WithMany(a => a.OpcionesSeleccionadas)
            .HasForeignKey(r => new
            {
                r.RespuestaAlumnoId,
                r.PreguntaId
            })
            .HasPrincipalKey(a => new
            {
                a.Id,
                a.PreguntaId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RespuestaAlumnoOpcion>()
            .HasOne(r => r.OpcionPregunta)
            .WithMany()
            .HasForeignKey(r => new
            {
                r.OpcionPreguntaId,
                r.PreguntaId
            })
            .HasPrincipalKey(o => new
            {
                o.Id,
                o.PreguntaId
            })
            .OnDelete(DeleteBehavior.Restrict);

        // Entregas
        builder.Entity<Entrega>()
            .ToTable(tabla =>
            {
                tabla.HasCheckConstraint(
                    "CK_Entregas_NumeroEnvio",
                    "\"NumeroEnvio\" >= 1");

                tabla.HasCheckConstraint(
                    "CK_Entregas_Calificacion",
                    "\"Calificacion\" IS NULL OR \"Calificacion\" >= 0");
            });

        // Cuestionarios
        builder.Entity<Cuestionario>()
            .ToTable(tabla =>
            {
                tabla.HasCheckConstraint(
                    "CK_Cuestionarios_Intentos",
                    "\"MaximoIntentos\" >= 0");

                tabla.HasCheckConstraint(
                    "CK_Cuestionarios_Tiempo",
                    "\"TiempoGeneralSegundos\" IS NULL OR " +
                    "\"TiempoGeneralSegundos\" > 0");
            });

        // Preguntas
        builder.Entity<Pregunta>()
            .ToTable(tabla =>
            {
                tabla.HasCheckConstraint(
                    "CK_Preguntas_Orden",
                    "\"Orden\" >= 1");

                tabla.HasCheckConstraint(
                    "CK_Preguntas_Punteo",
                    "\"Punteo\" > 0");

                tabla.HasCheckConstraint(
                    "CK_Preguntas_Tiempo",
                    "\"TiempoLimiteSegundos\" IS NULL OR " +
                    "\"TiempoLimiteSegundos\" > 0");
            });

        // Opciones de preguntas
        builder.Entity<OpcionPregunta>()
            .ToTable(tabla => tabla.HasCheckConstraint(
                "CK_OpcionesPreguntas_Orden",
                "\"Orden\" >= 1"));

        // Intentos
        builder.Entity<IntentoCuestionario>()
            .ToTable(tabla =>
            {
                tabla.HasCheckConstraint(
                    "CK_IntentosCuestionarios_NumeroIntento",
                    "\"NumeroIntento\" >= 1");

                tabla.HasCheckConstraint(
                    "CK_IntentosCuestionarios_Calificacion",
                    "\"Calificacion\" IS NULL OR \"Calificacion\" >= 0");

                tabla.HasCheckConstraint(
                    "CK_IntentosCuestionarios_Fechas",
                    "\"FechaFinalizacion\" IS NULL OR " +
                    "\"FechaFinalizacion\" >= \"FechaInicio\"");
            });

        // Respuestas de alumnos
        builder.Entity<RespuestaAlumno>()
            .ToTable(tabla =>
            {
                tabla.HasCheckConstraint(
                    "CK_RespuestasAlumnos_Punteo",
                    "\"PunteoObtenido\" IS NULL OR \"PunteoObtenido\" >= 0");

                tabla.HasCheckConstraint(
                    "CK_RespuestasAlumnos_Fechas",
                    "\"FechaRespuesta\" IS NULL OR " +
                    "\"FechaRespuesta\" >= \"FechaPresentacion\"");
            });

        // Configuraciones de evaluación

        builder.Entity<ConfiguracionEvaluacion>()
            .HasAlternateKey(c => new
            {
                c.Id,
                c.AsignacionId
            });

        builder.Entity<ConfiguracionEvaluacion>()
            .HasIndex(c => new
            {
                c.AsignacionId,
                c.PeriodoId
            })
            .IsUnique();

        builder.Entity<ConfiguracionEvaluacion>()
            .HasOne(c => c.Asignacion)
            .WithMany()
            .HasForeignKey(c => new
            {
                c.AsignacionId,
                c.SeccionId
            })
            .HasPrincipalKey(a => new
            {
                a.Id,
                a.SeccionId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ConfiguracionEvaluacion>()
            .HasOne<Seccion>()
            .WithMany()
            .HasForeignKey(c => new
            {
                c.SeccionId,
                c.CicloEscolarId
            })
            .HasPrincipalKey(s => new
            {
                s.Id,
                s.CicloEscolarId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ConfiguracionEvaluacion>()
            .HasOne(c => c.Periodo)
            .WithMany()
            .HasForeignKey(c => new
            {
                c.PeriodoId,
                c.CicloEscolarId
            })
            .HasPrincipalKey(p => new
            {
                p.Id,
                p.CicloEscolarId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ConfiguracionEvaluacion>()
            .HasOne(c => c.ConfiguradoPorUsuario)
            .WithMany()
            .HasForeignKey(c => c.ConfiguradoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);


        // Categorías de evaluación

        builder.Entity<CategoriaEvaluacion>()
            .HasAlternateKey(c => new
            {
                c.Id,
                c.ConfiguracionEvaluacionId
            });

        builder.Entity<CategoriaEvaluacion>()
            .HasIndex(c => new
            {
                c.ConfiguracionEvaluacionId,
                c.Nombre
            })
            .IsUnique();

        builder.Entity<CategoriaEvaluacion>()
            .HasIndex(c => new
            {
                c.ConfiguracionEvaluacionId,
                c.Orden
            })
            .IsUnique();

        builder.Entity<CategoriaEvaluacion>()
            .HasOne(c => c.ConfiguracionEvaluacion)
            .WithMany(e => e.Categorias)
            .HasForeignKey(c => c.ConfiguracionEvaluacionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Actividades evaluables

        builder.Entity<ActividadEvaluable>()
            .HasIndex(a => new
            {
                a.ConfiguracionEvaluacionId,
                a.Orden
            })
            .IsUnique();

        builder.Entity<ActividadEvaluable>()
            .HasIndex(a => a.TareaId)
            .IsUnique();

        builder.Entity<ActividadEvaluable>()
            .HasOne(a => a.ConfiguracionEvaluacion)
            .WithMany(c => c.Actividades)
            .HasForeignKey(a => new
            {
                a.ConfiguracionEvaluacionId,
                a.AsignacionId
            })
            .HasPrincipalKey(c => new
            {
                c.Id,
                c.AsignacionId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ActividadEvaluable>()
            .HasOne(a => a.CategoriaEvaluacion)
            .WithMany(c => c.Actividades)
            .HasForeignKey(a => new
            {
                a.CategoriaEvaluacionId,
                a.ConfiguracionEvaluacionId
            })
            .HasPrincipalKey(c => new
            {
                c.Id,
                c.ConfiguracionEvaluacionId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ActividadEvaluable>()
            .HasOne(a => a.Tarea)
            .WithMany()
            .HasForeignKey(a => new
            {
                a.TareaId,
                a.AsignacionId
            })
            .HasPrincipalKey(t => new
            {
                t.Id,
                t.AsignacionId
            })
            .OnDelete(DeleteBehavior.Restrict);

        // Calificaciones manuales

        builder.Entity<CalificacionManual>()
            .HasIndex(c => new
            {
                c.ActividadEvaluableId,
                c.AlumnoId
            })
            .IsUnique()
            .HasFilter("\"InscripcionId\" IS NULL");

        builder.Entity<CalificacionManual>()
            .HasIndex(c => new
            {
                c.ActividadEvaluableId,
                c.InscripcionId
            })
            .IsUnique();

        builder.Entity<CalificacionManual>()
            .HasOne(c => c.ActividadEvaluable)
            .WithMany(a => a.CalificacionesManuales)
            .HasForeignKey(c => c.ActividadEvaluableId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CalificacionManual>()
            .HasOne(c => c.Alumno)
            .WithMany()
            .HasForeignKey(c => c.AlumnoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CalificacionManual>()
            .HasOne(c => c.Inscripcion)
            .WithMany()
            .HasForeignKey(c => new { c.InscripcionId, c.AlumnoId })
            .HasPrincipalKey(i => new { i.Id, i.AlumnoId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CalificacionManual>()
            .HasOne(c => c.CalificadoPorUsuario)
            .WithMany()
            .HasForeignKey(c => c.CalificadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configuración de nota anual

        builder.Entity<ConfiguracionNotaAnual>()
            .HasAlternateKey(c => new
            {
                c.Id,
                c.CicloEscolarId
            });

        builder.Entity<ConfiguracionNotaAnual>()
            .HasIndex(c => c.CicloEscolarId)
            .IsUnique();

        builder.Entity<ConfiguracionNotaAnual>()
            .HasOne(c => c.CicloEscolar)
            .WithMany()
            .HasForeignKey(c => c.CicloEscolarId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ConfiguracionNotaAnual>()
            .HasOne(c => c.ConfiguradoPorUsuario)
            .WithMany()
            .HasForeignKey(c => c.ConfiguradoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ponderaciones de períodos

        builder.Entity<PonderacionPeriodoAnual>()
            .HasIndex(p => new
            {
                p.ConfiguracionNotaAnualId,
                p.PeriodoId
            })
            .IsUnique();

        builder.Entity<PonderacionPeriodoAnual>()
            .HasOne(p => p.ConfiguracionNotaAnual)
            .WithMany(c => c.Ponderaciones)
            .HasForeignKey(p => new
            {
                p.ConfiguracionNotaAnualId,
                p.CicloEscolarId
            })
            .HasPrincipalKey(c => new
            {
                c.Id,
                c.CicloEscolarId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PonderacionPeriodoAnual>()
            .HasOne(p => p.Periodo)
            .WithMany()
            .HasForeignKey(p => new
            {
                p.PeriodoId,
                p.CicloEscolarId
            })
            .HasPrincipalKey(periodo => new
            {
                periodo.Id,
                periodo.CicloEscolarId
            })
            .OnDelete(DeleteBehavior.Restrict);

        // Cierre de calificaciones

        builder.Entity<CierreCalificaciones>()
            .HasIndex(c => c.ConfiguracionEvaluacionId)
            .IsUnique();

        builder.Entity<CierreCalificaciones>()
            .HasOne(c => c.ConfiguracionEvaluacion)
            .WithOne()
            .HasForeignKey<CierreCalificaciones>(
                c => c.ConfiguracionEvaluacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CierreCalificaciones>()
            .HasOne(c => c.CerradoPorUsuario)
            .WithMany()
            .HasForeignKey(c => c.CerradoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Movimientos de cierre

        builder.Entity<MovimientoCierreCalificaciones>()
            .HasIndex(m => new
            {
                m.CierreCalificacionesId,
                m.Fecha
            });

        builder.Entity<MovimientoCierreCalificaciones>()
            .HasOne(m => m.CierreCalificaciones)
            .WithMany(c => c.Movimientos)
            .HasForeignKey(m => m.CierreCalificacionesId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MovimientoCierreCalificaciones>()
            .HasOne(m => m.RealizadoPorUsuario)
            .WithMany()
            .HasForeignKey(m => m.RealizadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Historial de calificaciones

        builder.Entity<HistorialCalificacion>()
            .HasIndex(h => h.CalificacionManualId);

        builder.Entity<HistorialCalificacion>()
            .HasIndex(h => h.EntregaId);

        builder.Entity<HistorialCalificacion>()
            .HasIndex(h => h.IntentoCuestionarioId);

        builder.Entity<HistorialCalificacion>()
            .HasIndex(h => h.RespuestaAlumnoId);

        builder.Entity<HistorialCalificacion>()
            .HasIndex(h => h.FechaCambio);

        builder.Entity<HistorialCalificacion>()
            .HasOne(h => h.CalificacionManual)
            .WithMany()
            .HasForeignKey(h => h.CalificacionManualId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<HistorialCalificacion>()
            .HasOne(h => h.Entrega)
            .WithMany()
            .HasForeignKey(h => h.EntregaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<HistorialCalificacion>()
            .HasOne(h => h.IntentoCuestionario)
            .WithMany()
            .HasForeignKey(h => h.IntentoCuestionarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<HistorialCalificacion>()
            .HasOne(h => h.RespuestaAlumno)
            .WithMany()
            .HasForeignKey(h => h.RespuestaAlumnoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<HistorialCalificacion>()
            .HasOne(h => h.ModificadoPorUsuario)
            .WithMany()
            .HasForeignKey(h => h.ModificadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Plantillas reutilizables de evaluación

        builder.Entity<PlantillaEvaluacion>()
            .HasIndex(x => new { x.CursoId, x.Nombre })
            .IsUnique()
            .HasFilter("\"CursoId\" IS NOT NULL");

        builder.Entity<PlantillaEvaluacion>()
            .HasIndex(x => x.Nombre)
            .IsUnique()
            .HasFilter("\"CursoId\" IS NULL");

        builder.Entity<PlantillaEvaluacion>()
            .HasOne(x => x.Curso)
            .WithMany()
            .HasForeignKey(x => x.CursoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PlantillaEvaluacion>()
            .HasOne(x => x.CreadoPorUsuario)
            .WithMany()
            .HasForeignKey(x => x.CreadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Resultados bimestrales inmutables

        builder.Entity<ResultadoCalificacionPeriodo>()
            .HasAlternateKey(x => new
            {
                x.Id,
                x.InscripcionId,
                x.AlumnoId
            });

        builder.Entity<ResultadoCalificacionPeriodo>()
            .HasIndex(x => new { x.ConfiguracionEvaluacionId, x.InscripcionId })
            .IsUnique();

        builder.Entity<ResultadoCalificacionPeriodo>()
            .HasOne(x => x.ConfiguracionEvaluacion)
            .WithMany()
            .HasForeignKey(x => x.ConfiguracionEvaluacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ResultadoCalificacionPeriodo>()
            .HasOne(x => x.Inscripcion)
            .WithMany()
            .HasForeignKey(x => new { x.InscripcionId, x.AlumnoId })
            .HasPrincipalKey(i => new { i.Id, i.AlumnoId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ResultadoCalificacionPeriodo>()
            .HasOne(x => x.Alumno)
            .WithMany()
            .HasForeignKey(x => x.AlumnoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Solicitudes de corrección extraordinaria

        builder.Entity<SolicitudCorreccionCalificacion>()
            .HasIndex(x => new { x.ResultadoCalificacionPeriodoId, x.Estado });

        builder.Entity<SolicitudCorreccionCalificacion>()
            .Property(x => x.VersionConcurrencia)
            .IsConcurrencyToken();

        builder.Entity<SolicitudCorreccionCalificacion>()
            .HasOne(x => x.ResultadoCalificacionPeriodo)
            .WithMany()
            .HasForeignKey(x => new
            {
                x.ResultadoCalificacionPeriodoId,
                x.InscripcionId,
                x.AlumnoId
            })
            .HasPrincipalKey(x => new
            {
                x.Id,
                x.InscripcionId,
                x.AlumnoId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SolicitudCorreccionCalificacion>()
            .HasOne(x => x.ActividadEvaluable)
            .WithMany()
            .HasForeignKey(x => x.ActividadEvaluableId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SolicitudCorreccionCalificacion>()
            .HasOne(x => x.Inscripcion)
            .WithMany()
            .HasForeignKey(x => new { x.InscripcionId, x.AlumnoId })
            .HasPrincipalKey(i => new { i.Id, i.AlumnoId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SolicitudCorreccionCalificacion>()
            .HasOne(x => x.Alumno)
            .WithMany()
            .HasForeignKey(x => x.AlumnoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SolicitudCorreccionCalificacion>()
            .HasOne(x => x.SolicitadaPorUsuario)
            .WithMany()
            .HasForeignKey(x => x.SolicitadaPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SolicitudCorreccionCalificacion>()
            .HasOne(x => x.RevisadaPorUsuario)
            .WithMany()
            .HasForeignKey(x => x.RevisadaPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SolicitudCorreccionCalificacion>()
            .HasOne(x => x.AplicadaPorUsuario)
            .WithMany()
            .HasForeignKey(x => x.AplicadaPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restricciones de categorías

        builder.Entity<CategoriaEvaluacion>()
            .ToTable(tabla =>
            {
                tabla.HasCheckConstraint(
                    "CK_CategoriasEvaluacion_Porcentaje",
                    "\"Porcentaje\" >= 0 AND \"Porcentaje\" <= 100");

                tabla.HasCheckConstraint(
                    "CK_CategoriasEvaluacion_Orden",
                    "\"Orden\" >= 1");

                tabla.HasCheckConstraint(
                    "CK_CategoriasEvaluacion_Tipo",
                    "\"Tipo\" >= 1 AND \"Tipo\" <= 3");
            });

        // Restricciones de actividades

        builder.Entity<ActividadEvaluable>()
            .ToTable(tabla =>
            {
                tabla.HasCheckConstraint(
                    "CK_ActividadesEvaluables_Punteo",
                    "\"PunteoMaximo\" > 0");

                tabla.HasCheckConstraint(
                    "CK_ActividadesEvaluables_Orden",
                    "\"Orden\" >= 1");

                tabla.HasCheckConstraint(
                    "CK_ActividadesEvaluables_Origen",
                    "(\"Origen\" = 1 AND \"TareaId\" IS NOT NULL) OR " +
                    "(\"Origen\" = 2 AND \"TareaId\" IS NULL)");
            });

        // Restricciones de calificaciones manuales

        builder.Entity<CalificacionManual>()
            .ToTable(tabla => tabla.HasCheckConstraint(
                "CK_CalificacionesManuales_Nota",
                "\"Nota\" IS NULL OR \"Nota\" >= 0"));

        // Restricciones de ponderaciones anuales

        builder.Entity<PonderacionPeriodoAnual>()
            .ToTable(tabla => tabla.HasCheckConstraint(
                "CK_PonderacionesPeriodosAnuales_Porcentaje",
                "\"Porcentaje\" >= 0 AND \"Porcentaje\" <= 100"));

        // Restricciones del historial

        builder.Entity<HistorialCalificacion>()
            .ToTable(tabla =>
            {
                tabla.HasCheckConstraint(
                    "CK_HistorialesCalificaciones_Origen",
                    "(\"TipoOrigen\" = 1 AND \"CalificacionManualId\" IS NOT NULL " +
                    "AND \"EntregaId\" IS NULL AND \"IntentoCuestionarioId\" IS NULL " +
                    "AND \"RespuestaAlumnoId\" IS NULL) OR " +
                    "(\"TipoOrigen\" = 2 AND \"EntregaId\" IS NOT NULL " +
                    "AND \"CalificacionManualId\" IS NULL AND \"IntentoCuestionarioId\" IS NULL " +
                    "AND \"RespuestaAlumnoId\" IS NULL) OR " +
                    "(\"TipoOrigen\" = 3 AND \"IntentoCuestionarioId\" IS NOT NULL " +
                    "AND \"CalificacionManualId\" IS NULL AND \"EntregaId\" IS NULL " +
                    "AND \"RespuestaAlumnoId\" IS NULL) OR " +
                    "(\"TipoOrigen\" = 4 AND \"RespuestaAlumnoId\" IS NOT NULL " +
                    "AND \"CalificacionManualId\" IS NULL AND \"EntregaId\" IS NULL " +
                    "AND \"IntentoCuestionarioId\" IS NULL)");

                tabla.HasCheckConstraint(
                    "CK_HistorialesCalificaciones_Notas",
                    "(\"NotaAnterior\" IS NULL OR \"NotaAnterior\" >= 0) AND " +
                    "(\"NotaNueva\" IS NULL OR \"NotaNueva\" >= 0)");
            });

        builder.Entity<PlantillaEvaluacion>()
            .ToTable(tabla => tabla.HasCheckConstraint(
                "CK_PlantillasEvaluacion_Definicion",
                "length(\"DefinicionJson\") > 0"));

        builder.Entity<ResultadoCalificacionPeriodo>()
            .ToTable(tabla => tabla.HasCheckConstraint(
                "CK_ResultadosCalificacionesPeriodos_Notas",
                "\"Desempeno\" >= 0 AND \"Actitudinal\" >= 0 AND " +
                "\"NotaBimestral\" >= 0 AND \"AbacusDesempeno\" >= 0 AND " +
                "\"AbacusActitudinal\" >= 0"));

        builder.Entity<SolicitudCorreccionCalificacion>()
            .ToTable(tabla =>
            {
                tabla.HasCheckConstraint(
                    "CK_SolicitudesCorreccionesCalificaciones_Notas",
                    "\"NotaAnterior\" >= 0 AND \"NotaPropuesta\" >= 0");
                tabla.HasCheckConstraint(
                    "CK_SolicitudesCorreccionesCalificaciones_Estado",
                    "(\"Estado\" = 1 AND \"RevisadaPorUsuarioId\" IS NULL AND " +
                    "\"FechaRevision\" IS NULL AND \"AplicadaPorUsuarioId\" IS NULL AND " +
                    "\"FechaAplicacion\" IS NULL) OR " +
                    "(\"Estado\" IN (2, 3) AND \"RevisadaPorUsuarioId\" IS NOT NULL AND " +
                    "\"FechaRevision\" IS NOT NULL AND \"AplicadaPorUsuarioId\" IS NULL AND " +
                    "\"FechaAplicacion\" IS NULL) OR " +
                    "(\"Estado\" = 4 AND \"RevisadaPorUsuarioId\" IS NOT NULL AND " +
                    "\"FechaRevision\" IS NOT NULL AND \"AplicadaPorUsuarioId\" IS NOT NULL AND " +
                    "\"FechaAplicacion\" IS NOT NULL)");
            });
    }
}

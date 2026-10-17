using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Models;

var connectionString = Environment.GetEnvironmentVariable("E2E_CONNECTION")
    ?? throw new InvalidOperationException("Defina E2E_CONNECTION para el entorno temporal.");

var options = new DbContextOptionsBuilder<Contexto>()
    .UseNpgsql(connectionString)
    .Options;

await using var db = new Contexto(options);
var now = DateTime.UtcNow;

async Task<Usuario> CrearUsuarioAsync(string usuario, string correo, string nombre, string apellido, string rol)
{
    var existente = await db.Users.SingleOrDefaultAsync(x => x.UserName == usuario);
    if (existente is not null) return existente;

    var entidad = new Usuario
    {
        UserName = usuario,
        NormalizedUserName = usuario.ToUpperInvariant(),
        Email = correo,
        NormalizedEmail = correo.ToUpperInvariant(),
        EmailConfirmed = true,
        PrimerNombre = nombre,
        PrimerApellido = apellido,
        Activo = true,
        CambiarContrasena = false,
        FechaCreacion = now,
        FechaUltimoCambioContrasena = now,
        SecurityStamp = Guid.NewGuid().ToString(),
        ConcurrencyStamp = Guid.NewGuid().ToString(),
        LockoutEnabled = true
    };
    entidad.PasswordHash = new PasswordHasher<Usuario>().HashPassword(entidad, "Demo1!E2E");
    var role = await db.Roles.SingleAsync(x => x.Name == rol);
    db.Users.Add(entidad);
    db.UserRoles.Add(new IdentityUserRole<string> { UserId = entidad.Id, RoleId = role.Id });
    await db.SaveChangesAsync();
    return entidad;
}

var docenteUsuario = await CrearUsuarioAsync("demo.docente", "demo.docente@example.test", "Docente", "E2E", "Docente");
var docenteAjenoUsuario = await CrearUsuarioAsync("demo.docente.ajeno", "demo.docente.ajeno@example.test", "Docente", "AjenoE2E", "Docente");
var alumnoUsuario = await CrearUsuarioAsync("demo.alumno", "demo.alumno@example.test", "Alumno", "E2E", "Alumno");
var alumnoAjenoUsuario = await CrearUsuarioAsync("demo.alumno.ajeno", "demo.alumno.ajeno@example.test", "Alumno", "AjenoE2E", "Alumno");

if (!await db.CiclosEscolares.AnyAsync(x => x.Anio == 2099))
{
    var ciclo = new CicloEscolar { Anio = 2099, FechaInicio = new DateOnly(2099, 1, 1), FechaFin = new DateOnly(2099, 12, 31), Activo = true, Estado = EstadoCicloEscolar.Activo };
    var grado = new Grado { Nombre = "E2E", Carrera = "General", Orden = 1, Nivel = NivelEducativo.Basico };
    db.AddRange(ciclo, grado);
    await db.SaveChangesAsync();
    var periodo = new Periodo { CicloEscolarId = ciclo.Id, Numero = 1, Nombre = "Primer período", FechaInicio = ciclo.FechaInicio, FechaFin = ciclo.FechaFin };
    var seccion = new Seccion { CicloEscolarId = ciclo.Id, GradoId = grado.Id, Nombre = "A", Estado = EstadoRegistro.Activo };
    var seccionAjena = new Seccion { CicloEscolarId = ciclo.Id, GradoId = grado.Id, Nombre = "B", Estado = EstadoRegistro.Activo };
    var curso = new Curso { GradoId = grado.Id, Nombre = "Curso E2E", Estado = EstadoRegistro.Activo };
    var cursoAjeno = new Curso { GradoId = grado.Id, Nombre = "Curso ajeno E2E", Estado = EstadoRegistro.Activo };
    var docente = new Docente { UsuarioId = docenteUsuario.Id, Carnet = "E2E-DOC" };
    var docenteAjeno = new Docente { UsuarioId = docenteAjenoUsuario.Id, Carnet = "E2E-DOC-AJENO" };
    var alumno = new Alumno { UsuarioId = alumnoUsuario.Id, CodigoPersonal = "E2E-ALU" };
    var alumnoAjeno = new Alumno { UsuarioId = alumnoAjenoUsuario.Id, CodigoPersonal = "E2E-ALU-B" };
    db.AddRange(periodo, seccion, seccionAjena, curso, cursoAjeno, docente, docenteAjeno, alumno, alumnoAjeno);
    await db.SaveChangesAsync();
    var asignacion = new Asignacion { SeccionId = seccion.Id, CursoId = curso.Id, GradoId = grado.Id, DocenteId = docente.Id, Estado = EstadoRegistro.Activo, FechaInicioVigencia = now };
    var asignacionAjena = new Asignacion { SeccionId = seccionAjena.Id, CursoId = cursoAjeno.Id, GradoId = grado.Id, DocenteId = docente.Id, Estado = EstadoRegistro.Activo, FechaInicioVigencia = now };
    var inscripcion = new Inscripcion { AlumnoId = alumno.Id, SeccionId = seccion.Id, CicloEscolarId = ciclo.Id, Fecha = ciclo.FechaInicio, Estado = EstadoInscripcion.Activa };
    var inscripcionAjena = new Inscripcion { AlumnoId = alumnoAjeno.Id, SeccionId = seccionAjena.Id, CicloEscolarId = ciclo.Id, Fecha = ciclo.FechaInicio, Estado = EstadoInscripcion.Activa };
    db.AddRange(asignacion, asignacionAjena, inscripcion, inscripcionAjena);
    await db.SaveChangesAsync();
}

if (!await db.Planificaciones.AnyAsync(x => x.Titulo == "Planificación propia IA E2E"))
{
    var docente = await db.Docentes.SingleAsync(x => x.UsuarioId == docenteUsuario.Id);
    var asignacion = await db.Asignaciones.SingleAsync(x => x.DocenteId == docente.Id && x.Seccion.Nombre == "A");
    var planificacion = new Planificacion
    {
        GrupoVersionId = Guid.NewGuid(),
        DocenteId = docente.Id,
        Tipo = TipoPlanificacion.Semanal,
        Titulo = "Planificación propia IA E2E",
        Objetivos = "Precondición ficticia para validar acceso contextual de IA.",
        FechaCreacion = now
    };
    db.Planificaciones.Add(planificacion);
    await db.SaveChangesAsync();
    db.PlanificacionesAsignaciones.Add(new PlanificacionAsignacion
    {
        PlanificacionId = planificacion.Id,
        AsignacionId = asignacion.Id,
        FechaInicio = new DateOnly(2099, 1, 1),
        FechaFin = new DateOnly(2099, 12, 31)
    });
    await db.SaveChangesAsync();
}

Console.WriteLine("Escenario E2E preparado: demo.docente / demo.alumno (contraseña Demo1!E2E).");

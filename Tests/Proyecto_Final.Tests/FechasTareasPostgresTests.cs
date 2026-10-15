using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Models;
using Proyecto_Final.Servicios.Archivos;
using Proyecto_Final.Servicios.Comunes;
using Proyecto_Final.Servicios.GestionTareas;
using Proyecto_Final.ViewModels.Tareas;
using T = Proyecto_Final.Tests.CuestionarioServicioTests;

namespace Proyecto_Final.Tests;

// Las fechas de los formularios llegan sin zona horaria (hora de Guatemala).
// PostgreSQL solo acepta UTC en "timestamp with time zone".
public sealed class FechasTareasPostgresTests
{
    private static readonly string? Conexion =
        Environment.GetEnvironmentVariable("SABIA_PRUEBAS_PG");

    [Fact]
    public void HoraGuatemala_ConvierteEnAmbosSentidos()
    {
        var escrita = new DateTime(2026, 10, 13, 8, 30, 0, DateTimeKind.Unspecified);

        var utc = HoraGuatemala.ParaGuardar(escrita);

        Assert.Equal(DateTimeKind.Utc, utc.Kind);
        Assert.Equal(new DateTime(2026, 10, 13, 14, 30, 0, DateTimeKind.Utc), utc);
        Assert.Equal(escrita, HoraGuatemala.ParaFormulario(utc));
        Assert.Null(HoraGuatemala.ParaGuardar((DateTime?)null));
    }

    [Fact]
    public async Task Postgres_CrearYEditarTareaConFechasDelFormulario()
    {
        Assert.SkipWhen(string.IsNullOrWhiteSpace(Conexion) ||
            !CuestionarioPostgresTests.DestinoConfirmado,
            "PostgreSQL descartable no configurado o no confirmado.");

        await using var baseDatos = await CuestionarioPostgresTests.BasePrueba.CrearAsync(Conexion!);
        await using (var contexto = baseDatos.Contexto())
        {
            await T.SembrarAsync(contexto, _ => { }, sinCuestionario: true);

            // El escenario usa ids fijos; se ajusta la secuencia para nuevas tareas.
            await contexto.Database.ExecuteSqlRawAsync(
                "SELECT setval(pg_get_serial_sequence('\"Tareas\"', 'Id'), (SELECT MAX(\"Id\") FROM \"Tareas\"))",
                TestContext.Current.CancellationToken);
        }

        var disponible = new DateTime(2026, 10, 13, 7, 0, 0, DateTimeKind.Unspecified);
        var limite = new DateTime(2026, 10, 14, 23, 59, 0, DateTimeKind.Unspecified);

        await using (var contexto = baseDatos.Contexto())
        {
            var servicio = new TareaServicio(contexto, new ArchivosSinUso(), new AccesoDocente());
            var resultado = await servicio.CrearAsync(new CrearTarea
            {
                AsignacionId = 1,
                Tipo = TipoTarea.Cuestionario,
                TokenCreacion = Guid.NewGuid(),
                Titulo = "Tarea con fechas",
                PunteoMaximo = 10,
                FechaDisponibilidad = disponible,
                FechaLimite = limite
            }, TestContext.Current.CancellationToken);

            Assert.True(resultado.Exitoso, resultado.Mensaje);
        }

        int id;
        await using (var contexto = baseDatos.Contexto())
        {
            var tarea = await contexto.Tareas.SingleAsync(x => x.Titulo == "Tarea con fechas", TestContext.Current.CancellationToken);
            id = tarea.Id;
            Assert.Equal(new DateTime(2026, 10, 15, 5, 59, 0, DateTimeKind.Utc), tarea.FechaLimite);
            Assert.Equal(new DateTime(2026, 10, 13, 13, 0, 0, DateTimeKind.Utc), tarea.FechaDisponibilidad);
        }

        await using (var contexto = baseDatos.Contexto())
        {
            var servicio = new TareaServicio(contexto, new ArchivosSinUso(), new AccesoDocente());
            var edicion = await servicio.ObtenerEditarAsync(id);

            // El formulario de edición muestra la misma hora que escribió el docente.
            Assert.NotNull(edicion);
            Assert.Equal(disponible, edicion!.FechaDisponibilidad);
            Assert.Equal(limite, edicion.FechaLimite);

            edicion.FechaLimite = limite.AddDays(1);
            var resultado = await servicio.EditarAsync(edicion, TestContext.Current.CancellationToken);
            Assert.True(resultado.Exitoso, resultado.Mensaje);
        }

        await using (var contexto = baseDatos.Contexto())
        {
            var tarea = await contexto.Tareas.SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
            Assert.Equal(new DateTime(2026, 10, 16, 5, 59, 0, DateTimeKind.Utc), tarea.FechaLimite);
        }
    }

    private sealed class AccesoDocente : Servicios.SeguridadAcademica.IAccesoAcademicoServicio
    {
        public string? UsuarioId => "docente";
        public bool EsOperadorInstitucional => false;
        public bool TienePermiso(string permiso) => true;
        public Task<Servicios.SeguridadAcademica.ContextoAcademicoUsuario> ObtenerContextoUsuarioAsync() =>
            Task.FromResult(new Servicios.SeguridadAcademica.ContextoAcademicoUsuario(1, null));
        public Task<int?> ObtenerDocenteIdAsync() => Task.FromResult<int?>(1);
        public Task<int?> ObtenerAlumnoIdAsync() => Task.FromResult<int?>(null);
        public Task<bool> EsDocenteDeAsignacionAsync(int asignacionId) => Task.FromResult(true);
        public Task<bool> AlumnoPerteneceAsignacionAsync(int asignacionId) => Task.FromResult(false);
        public Task<bool> PuedeConsultarAsignacionVigenteAsync(int asignacionId) => Task.FromResult(true);
        public Task<bool> PuedeConsultarAsignacionHistoricaAsync(int asignacionId) => Task.FromResult(true);
        public Task<bool> PuedeGestionarAsignacionVigenteAsync(int asignacionId) => Task.FromResult(true);
    }

    private sealed class ArchivosSinUso : IArchivoFisicoServicio
    {
        public Task<ResultadoOperacion<ArchivoGuardado>> GuardarAsync(IFormFile archivo, string categoria, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("La prueba no adjunta archivos.");
        public Task<Stream?> AbrirLecturaAsync(string claveAlmacenamiento, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(null);
        public Task EliminarAsync(string claveAlmacenamiento, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}

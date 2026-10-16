using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Proyecto_Final.Data;
using Proyecto_Final.Models;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.GestionNotificaciones;
using Proyecto_Final.Servicios.SeguridadAcademica;

namespace Proyecto_Final.Tests;

public sealed class NotificacionServicioTests
{
    [Fact]
    public async Task Alumno_NovedadSeCuentaHastaPrimeraVisualizacion_YNoDuplicaCuestionario()
    {
        await using var contexto = CrearContexto();
        await CuestionarioServicioTests.SembrarAsync(contexto, _ => { });
        var servicio = new NotificacionServicio(contexto, new AccesoPrueba(alumnoId: 1));

        var antes = await servicio.ObtenerResumenAsync();
        Assert.Equal(1, antes.NovedadesNoVisualizadas);
        Assert.Single(antes.Notificaciones.Where(x => x.EsNovedad));
        Assert.Equal("Cuestionarios", antes.Notificaciones.Single(x => x.EsNovedad).Controlador);

        await servicio.RegistrarVisualizacionActividadAsync(1);
        await servicio.RegistrarVisualizacionActividadAsync(1);

        var despues = await servicio.ObtenerResumenAsync();
        Assert.Equal(0, despues.NovedadesNoVisualizadas);
        Assert.Equal(1, await contexto.VisualizacionesActividades.CountAsync());
    }

    [Fact]
    public async Task Alumno_InactivoONoRelacionado_NoRecibeNiPuedeRegistrarNovedad()
    {
        await using var contexto = CrearContexto();
        await CuestionarioServicioTests.SembrarAsync(contexto, _ => { });
        var inscripcion = await contexto.Inscripciones.SingleAsync();
        inscripcion.Estado = EstadoInscripcion.Retirada;
        await contexto.SaveChangesAsync();
        var servicio = new NotificacionServicio(contexto, new AccesoPrueba(alumnoId: 1));

        Assert.Equal(0, (await servicio.ObtenerResumenAsync()).NovedadesNoVisualizadas);
        await servicio.RegistrarVisualizacionActividadAsync(1);
        Assert.Empty(await contexto.VisualizacionesActividades.ToListAsync());
    }

    [Fact]
    public async Task Docente_SoloMuestraEntregasAutorizadas_YRespetaPermisoVer()
    {
        await using var contexto = CrearContexto();
        await CuestionarioServicioTests.SembrarAsync(contexto, _ => { });
        var entrega = await contexto.Entregas.SingleAsync();
        entrega.Estado = EstadoEntrega.Enviada;
        await contexto.SaveChangesAsync();

        var conPermiso = new NotificacionServicio(contexto, new AccesoPrueba(docenteId: 1));
        Assert.Contains((await conPermiso.ObtenerAsync()), x => x.Titulo == "Entregas por revisar");

        var sinPermiso = new NotificacionServicio(contexto,
            new AccesoPrueba(docenteId: 1, permisos: []));
        Assert.DoesNotContain((await sinPermiso.ObtenerAsync()), x => x.Titulo == "Entregas por revisar");
    }

    private static Contexto CrearContexto() => new(new DbContextOptionsBuilder<Contexto>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);

    private sealed class AccesoPrueba(
        int? alumnoId = null,
        int? docenteId = null,
        string[]? permisos = null) : IAccesoAcademicoServicio
    {
        private readonly HashSet<string> permitidos = permisos == null
            ? [Permisos.Entregas.Ver, Permisos.Entregas.Entregar, Permisos.Entregas.Calificar,
                Permisos.Cuestionarios.Ver, Permisos.Cuestionarios.Resolver,
                Permisos.Planificaciones.Ver, Permisos.Asignaciones.Ver, Permisos.Notificaciones.Ver]
            : permisos.ToHashSet(StringComparer.Ordinal);
        public string? UsuarioId => alumnoId.HasValue ? "alumno" : "docente";
        public bool EsOperadorInstitucional => false;
        public bool TienePermiso(string permiso) => permitidos.Contains(permiso);
        public Task<ContextoAcademicoUsuario> ObtenerContextoUsuarioAsync() => Task.FromResult(new ContextoAcademicoUsuario(docenteId, alumnoId));
        public Task<int?> ObtenerDocenteIdAsync() => Task.FromResult(docenteId);
        public Task<int?> ObtenerAlumnoIdAsync() => Task.FromResult(alumnoId);
        public Task<bool> EsDocenteDeAsignacionAsync(int id) => Task.FromResult(docenteId.HasValue);
        public Task<bool> AlumnoPerteneceAsignacionAsync(int id) => Task.FromResult(alumnoId.HasValue);
        public Task<bool> PuedeConsultarAsignacionVigenteAsync(int id) => Task.FromResult(true);
        public Task<bool> PuedeConsultarAsignacionHistoricaAsync(int id) => Task.FromResult(true);
        public Task<bool> PuedeGestionarAsignacionVigenteAsync(int id) => Task.FromResult(true);
    }
}

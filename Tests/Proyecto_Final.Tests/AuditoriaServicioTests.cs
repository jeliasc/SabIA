using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Proyecto_Final.Data;
using Proyecto_Final.Controllers;
using Proyecto_Final.Models;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Servicios.Auditoria;
using Proyecto_Final.ViewModels.Auditoria;

namespace Proyecto_Final.Tests;

public sealed class AuditoriaServicioTests
{
    [Fact]
    public void CrearRegistro_CapturaContextoTecnico_SinQueryString()
    {
        using var proveedor = CrearProveedor(out var accesor);
        using var alcance = proveedor.CreateScope();
        var http = CrearHttp("usuario-1", Roles.Administrador);
        http.Connection.RemoteIpAddress = IPAddress.Parse("2001:db8::25");
        http.Request.Method = "POST";
        http.Request.PathBase = "/sabia";
        http.Request.Path = "/Usuarios/Editar/1";
        http.Request.QueryString = new QueryString("?token=secreto");
        http.Request.Headers.UserAgent = new string('x', 600);
        http.TraceIdentifier = new string('c', 80);
        accesor.HttpContext = http;
        var servicio = CrearServicio(alcance, proveedor, accesor);

        var registro = servicio.CrearRegistro(
            "Usuarios", "Editar", TipoEventoAuditoria.Operacion,
            ResultadoAuditoria.Exitoso, "Usuario actualizado");

        Assert.Equal("usuario-1", registro.UsuarioId);
        Assert.Equal("2001:db8::25", registro.DireccionIp);
        Assert.Equal("/sabia/Usuarios/Editar/1", registro.Ruta);
        Assert.DoesNotContain("token", registro.Ruta);
        Assert.Equal("POST", registro.MetodoHttp);
        Assert.Equal(512, registro.UserAgent!.Length);
        Assert.Equal(64, registro.CorrelationId!.Length);
    }

    [Fact]
    public void CrearRegistro_SinSolicitudHttp_PermiteDatosTecnicosNulos()
    {
        using var proveedor = CrearProveedor(out var accesor);
        using var alcance = proveedor.CreateScope();
        var servicio = CrearServicio(alcance, proveedor, accesor);

        var registro = servicio.CrearRegistro(
            "Procesos", "TareaProgramada", TipoEventoAuditoria.Operacion,
            ResultadoAuditoria.Exitoso, "Proceso completado");

        Assert.Null(registro.DireccionIp);
        Assert.Null(registro.UserAgent);
        Assert.Null(registro.Ruta);
        Assert.Null(registro.MetodoHttp);
        Assert.Null(registro.CorrelationId);
    }

    [Theory]
    [InlineData("192.0.2.50")]
    [InlineData("2001:db8::50")]
    public void CrearRegistro_CapturaIpv4EIpv6(string direccion)
    {
        using var proveedor = CrearProveedor(out var accesor);
        using var alcance = proveedor.CreateScope();
        accesor.HttpContext = CrearHttp("usuario-1", Roles.Administrador);
        accesor.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse(direccion);
        var servicio = CrearServicio(alcance, proveedor, accesor);

        var registro = servicio.CrearRegistro(
            "Usuarios", "Consultar", TipoEventoAuditoria.Operacion,
            ResultadoAuditoria.Exitoso, "Consulta autorizada");

        Assert.Equal(direccion, registro.DireccionIp);
    }

    [Fact]
    public void CrearRegistro_OmiteValoresQuePuedenContenerSecretos()
    {
        using var proveedor = CrearProveedor(out var accesor);
        using var alcance = proveedor.CreateScope();
        var servicio = CrearServicio(alcance, proveedor, accesor);

        var registro = servicio.CrearRegistro(
            "Usuarios", "Crear", TipoEventoAuditoria.Operacion,
            ResultadoAuditoria.Exitoso, "Usuario creado",
            valoresNuevos: "{\"password\":\"no-debe-guardarse\"}");

        Assert.Equal("[Contenido omitido por seguridad]", registro.ValoresNuevos);
    }

    [Fact]
    public async Task AgregarATransaccion_NoPersisteHastaQueLaOperacionGuarda()
    {
        using var proveedor = CrearProveedor(out var accesor);
        using var alcance = proveedor.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<Contexto>();
        var servicio = CrearServicio(alcance, proveedor, accesor);
        var registro = servicio.CrearRegistro(
            "Roles", "Editar", TipoEventoAuditoria.Operacion,
            ResultadoAuditoria.Exitoso, "Rol actualizado");

        servicio.AgregarATransaccion(registro);

        using (var verificacionAntes = proveedor.CreateScope())
            Assert.Empty(await verificacionAntes.ServiceProvider.GetRequiredService<Contexto>()
                .RegistrosAuditoria.ToListAsync(TestContext.Current.CancellationToken));

        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        using var verificacionDespues = proveedor.CreateScope();
        Assert.Single(await verificacionDespues.ServiceProvider.GetRequiredService<Contexto>()
            .RegistrosAuditoria.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RegistrarIndependienteAsync_PersisteRechazoEnContextoSeparado()
    {
        using var proveedor = CrearProveedor(out var accesor);
        using var alcance = proveedor.CreateScope();
        var servicio = CrearServicio(alcance, proveedor, accesor);
        var registro = servicio.CrearRegistro(
            "Autenticacion", "IniciarSesion", TipoEventoAuditoria.Seguridad,
            ResultadoAuditoria.Rechazado, "Credenciales inválidas");

        await servicio.RegistrarIndependienteAsync(registro);

        using var verificacion = proveedor.CreateScope();
        var guardado = await verificacion.ServiceProvider.GetRequiredService<Contexto>()
            .RegistrosAuditoria.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ResultadoAuditoria.Rechazado, guardado.Resultado);
    }

    [Fact]
    public async Task Consulta_ExigeSuperusuario_YPaginaEnServidor()
    {
        using var proveedor = CrearProveedor(out var accesor);
        using var alcance = proveedor.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<Contexto>();
        contexto.RegistrosAuditoria.AddRange(Enumerable.Range(1, 30).Select(i => new RegistroAuditoria
        {
            FechaUtc = DateTime.UtcNow.AddMinutes(-i),
            Modulo = i <= 28 ? "Usuarios" : "Roles",
            Accion = "Consultar",
            Tipo = TipoEventoAuditoria.Operacion,
            Resultado = ResultadoAuditoria.Exitoso,
            Descripcion = $"Evento {i}"
        }));
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        var consulta = new AuditoriaConsultaServicio(contexto, accesor);

        accesor.HttpContext = CrearHttp("administrador", Roles.Administrador);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            consulta.ObtenerPaginaAsync(new AuditoriaFiltro()));

        accesor.HttpContext = CrearHttp("superusuario", Roles.Superusuario);
        var primera = await consulta.ObtenerPaginaAsync(new AuditoriaFiltro { Modulo = "Usuarios" });
        var segunda = await consulta.ObtenerPaginaAsync(new AuditoriaFiltro { Modulo = "Usuarios", Pagina = 2 });

        Assert.Equal(28, primera.TotalRegistros);
        Assert.Equal(25, primera.Registros.Count);
        Assert.Equal(3, segunda.Registros.Count);
    }

    [Fact]
    public async Task Consulta_AplicaFiltrosCombinados_YDevuelveDetalleTecnico()
    {
        using var proveedor = CrearProveedor(out var accesor);
        using var alcance = proveedor.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<Contexto>();
        var responsable = new Usuario
        {
            Id = "actor-1",
            UserName = "ana",
            Email = "ana@example.test",
            PrimerNombre = "Ana",
            PrimerApellido = "Prueba"
        };
        contexto.Users.Add(responsable);
        contexto.RegistrosAuditoria.AddRange(
            new RegistroAuditoria
            {
                FechaUtc = new DateTime(2026, 10, 1, 15, 0, 0, DateTimeKind.Utc),
                Usuario = responsable,
                Modulo = "Usuarios",
                Accion = "Desactivar",
                Tipo = TipoEventoAuditoria.Seguridad,
                Resultado = ResultadoAuditoria.Rechazado,
                Descripcion = "Operación rechazada",
                DireccionIp = "192.0.2.44",
                UserAgent = "Navegador de prueba",
                Ruta = "/Usuarios/Desactivar/1",
                MetodoHttp = "POST",
                CorrelationId = "correlacion-1"
            },
            new RegistroAuditoria
            {
                FechaUtc = new DateTime(2026, 10, 2, 15, 0, 0, DateTimeKind.Utc),
                Modulo = "Roles",
                Accion = "Editar",
                Tipo = TipoEventoAuditoria.Operacion,
                Resultado = ResultadoAuditoria.Exitoso,
                Descripcion = "Otro evento"
            });
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        accesor.HttpContext = CrearHttp("superusuario", Roles.Superusuario);
        var consulta = new AuditoriaConsultaServicio(contexto, accesor);

        var pagina = await consulta.ObtenerPaginaAsync(new AuditoriaFiltro
        {
            FechaDesde = new DateOnly(2026, 10, 1),
            FechaHasta = new DateOnly(2026, 10, 1),
            Responsable = "ana@example",
            Modulo = "Usuarios",
            Accion = "Desactivar",
            DireccionIp = "192.0.2",
            Resultado = ResultadoAuditoria.Rechazado
        });
        var detalle = await consulta.ObtenerDetalleAsync(Assert.Single(pagina.Registros).Id);

        Assert.NotNull(detalle);
        Assert.Equal("Navegador de prueba", detalle.UserAgent);
        Assert.Equal("/Usuarios/Desactivar/1", detalle.Ruta);
        Assert.Equal("correlacion-1", detalle.CorrelationId);
    }

    [Fact]
    public void ZonaHoraria_ConvierteUtcAGuatemala_CercaDeMedianoche()
    {
        var utc = new DateTime(2026, 10, 10, 2, 32, 38, DateTimeKind.Utc);

        var guatemala = ZonaHorariaAuditoria.ConvertirUtcAGuatemala(utc);

        Assert.Equal(new DateTime(2026, 10, 9, 20, 32, 38), guatemala);
        Assert.Equal(DateTimeKind.Unspecified, guatemala.Kind);
    }

    [Fact]
    public async Task Consulta_FiltroPorFecha_UsaLimitesDelDiaEnGuatemala()
    {
        using var proveedor = CrearProveedor(out var accesor);
        using var alcance = proveedor.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<Contexto>();
        contexto.RegistrosAuditoria.AddRange(
            Evento(new DateTime(2026, 10, 9, 5, 59, 59, DateTimeKind.Utc), "Antes"),
            Evento(new DateTime(2026, 10, 9, 6, 0, 0, DateTimeKind.Utc), "Inicio"),
            Evento(new DateTime(2026, 10, 10, 5, 59, 59, DateTimeKind.Utc), "Fin"),
            Evento(new DateTime(2026, 10, 10, 6, 0, 0, DateTimeKind.Utc), "Después"));
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        accesor.HttpContext = CrearHttp("superusuario", Roles.Superusuario);
        var consulta = new AuditoriaConsultaServicio(contexto, accesor);

        var pagina = await consulta.ObtenerPaginaAsync(new AuditoriaFiltro
        {
            FechaDesde = new DateOnly(2026, 10, 9),
            FechaHasta = new DateOnly(2026, 10, 9)
        });

        Assert.Equal(2, pagina.TotalRegistros);
        Assert.Contains(pagina.Registros, x => x.Accion == "Inicio");
        Assert.Contains(pagina.Registros, x => x.Accion == "Fin");
        Assert.DoesNotContain(pagina.Registros, x => x.Accion is "Antes" or "Después");
    }

    [Fact]
    public async Task Interceptor_RegistraCambioExitoso_SinSecretos()
    {
        var accesor = new HttpContextAccessor { HttpContext = CrearHttp("superusuario", Roles.Superusuario) };
        var interceptor = new AuditoriaSaveChangesInterceptor(accesor);
        var opciones = new DbContextOptionsBuilder<Contexto>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;
        await using var contexto = new Contexto(opciones);
        contexto.Users.Add(new Usuario
        {
            Id = "nuevo",
            UserName = "nuevo",
            PrimerNombre = "Nueva",
            PrimerApellido = "Persona",
            PasswordHash = "secreto"
        });

        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var registro = await contexto.RegistrosAuditoria.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Usuario", registro.Entidad);
        Assert.Equal(ResultadoAuditoria.Exitoso, registro.Resultado);
        Assert.Contains("Activo", registro.ValoresNuevos);
        Assert.DoesNotContain("PrimerNombre", registro.ValoresNuevos);
        Assert.DoesNotContain("PasswordHash", registro.ValoresNuevos);
        Assert.DoesNotContain("secreto", registro.ValoresNuevos);
    }

    [Fact]
    public async Task Interceptor_NoPersisteAuditoria_SiFallaLaOperacion()
    {
        var nombreBase = Guid.NewGuid().ToString();
        var auditoria = new AuditoriaSaveChangesInterceptor(new HttpContextAccessor());
        var opciones = new DbContextOptionsBuilder<Contexto>()
            .UseInMemoryDatabase(nombreBase)
            .AddInterceptors(auditoria, new FalloSaveChangesInterceptor())
            .Options;
        await using (var contexto = new Contexto(opciones))
        {
            contexto.Grados.Add(new Grado
            {
                Nombre = "Primero",
                Nivel = NivelEducativo.Basico,
                Orden = 1
            });
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                contexto.SaveChangesAsync(TestContext.Current.CancellationToken));
        }

        var opcionesVerificacion = new DbContextOptionsBuilder<Contexto>()
            .UseInMemoryDatabase(nombreBase)
            .Options;
        await using var verificacion = new Contexto(opcionesVerificacion);
        Assert.Empty(await verificacion.RegistrosAuditoria.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await verificacion.Grados.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Interceptor_IntegraEntidadAdministrativaYAcademica_SinDuplicar()
    {
        var interceptor = new AuditoriaSaveChangesInterceptor(new HttpContextAccessor());
        var opciones = new DbContextOptionsBuilder<Contexto>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;
        await using var contexto = new Contexto(opciones);
        contexto.Roles.Add(new Rol { Id = "rol-1", Name = "Coordinación", Activo = true });
        contexto.Grados.Add(new Grado
        {
            Nombre = "Primero",
            Carrera = "Básico",
            Nivel = NivelEducativo.Basico,
            Orden = 1
        });

        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);

        var registros = await contexto.RegistrosAuditoria.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, registros.Count);
        Assert.Contains(registros, x => x.Entidad == nameof(Rol));
        Assert.Contains(registros, x => x.Entidad == nameof(Grado));
    }

    [Fact]
    public void ControladorAuditoria_EsSoloLectura_YExclusivoSuperusuario()
    {
        var autorizacion = Assert.Single(typeof(AuditoriaController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>());
        Assert.Equal(Politicas.AuditoriaSuperusuario, autorizacion.Policy);

        var acciones = typeof(AuditoriaController)
            .GetMethods(System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.DeclaredOnly);
        Assert.Equal(2, acciones.Length);
        Assert.All(acciones, accion =>
            Assert.NotEmpty(accion.GetCustomAttributes(typeof(HttpGetAttribute), inherit: true)));
        Assert.DoesNotContain(acciones, x =>
            x.Name.Contains("Editar", StringComparison.OrdinalIgnoreCase) ||
            x.Name.Contains("Eliminar", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(true, "203.0.113.20")]
    [InlineData(false, "10.0.0.99")]
    public async Task EncabezadoReenviado_SoloSeAceptaDeProxyConfiable(
        bool proxyConfiable,
        string ipEsperada)
    {
        var opciones = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor,
            ForwardLimit = 1
        };
        opciones.KnownProxies.Add(IPAddress.Parse("10.0.0.10"));
        var middleware = new ForwardedHeadersMiddleware(
            _ => Task.CompletedTask,
            NullLoggerFactory.Instance,
            Options.Create(opciones));
        var contexto = new DefaultHttpContext();
        contexto.Connection.RemoteIpAddress = IPAddress.Parse(proxyConfiable ? "10.0.0.10" : "10.0.0.99");
        contexto.Request.Headers["X-Forwarded-For"] = "203.0.113.20";

        await middleware.Invoke(contexto);

        Assert.Equal(ipEsperada, contexto.Connection.RemoteIpAddress!.ToString());
    }

    private static ServiceProvider CrearProveedor(out HttpContextAccessor accesor)
    {
        accesor = new HttpContextAccessor();
        var servicios = new ServiceCollection();
        var baseDatos = Guid.NewGuid().ToString();
        servicios.AddSingleton<IHttpContextAccessor>(accesor);
        servicios.AddDbContext<Contexto>(opciones =>
            opciones.UseInMemoryDatabase(baseDatos));
        return servicios.BuildServiceProvider();
    }

    private static AuditoriaServicio CrearServicio(
        IServiceScope alcance,
        IServiceProvider proveedor,
        IHttpContextAccessor accesor) =>
        new(
            alcance.ServiceProvider.GetRequiredService<Contexto>(),
            accesor,
            proveedor.GetRequiredService<IServiceScopeFactory>());

    private static DefaultHttpContext CrearHttp(string usuarioId, string rol)
    {
        var contexto = new DefaultHttpContext();
        contexto.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, usuarioId),
            new Claim(ClaimTypes.Role, rol)
        ], "Prueba"));
        return contexto;
    }

    private static RegistroAuditoria Evento(DateTime fechaUtc, string accion) => new()
    {
        FechaUtc = fechaUtc,
        Modulo = "Pruebas",
        Accion = accion,
        Tipo = TipoEventoAuditoria.Operacion,
        Resultado = ResultadoAuditoria.Exitoso,
        Descripcion = accion
    };

    private sealed class FalloSaveChangesInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromException<InterceptionResult<int>>(
                new InvalidOperationException("Fallo simulado."));
    }
}

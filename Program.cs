using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Data.Seeders;
using Proyecto_Final.Models;
using Proyecto_Final.Seguridad;
using Proyecto_Final.Seguridad.Filtros;
using Proyecto_Final.Servicios.Correo;
using Proyecto_Final.Servicios.Usuarios;
using Proyecto_Final.Servicios.Autenticacion;
using Proyecto_Final.Servicios.GestionRoles;
using Proyecto_Final.Servicios.GestionPermisos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<Contexto>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("ConexionPrincipal")
    )
);



builder.Services.AddIdentity<Usuario, Rol>(options =>
    {
        // Requisitos de contraseña
        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        // Bloqueo por intentos fallidos
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

        // El correo no puede repetirse
        options.User.RequireUniqueEmail = true;

        // No exigimos confirmación por correo por ahora
        options.SignIn.RequireConfirmedEmail = false;
    })
    .AddEntityFrameworkStores<Contexto>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<ForzarCambioContrasenaFiltro>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login/Login";
    options.LogoutPath = "/Login/CerrarSesion";
    options.AccessDeniedPath = "/Login/AccesoDenegado";

    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;

    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddAuthorization(options =>
{
    // EXIGIR AUTENTICACIÓN EN TODO EL SISTEMA
    // excepto en las acciones marcadas con AllowAnonymous.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddSingleton<
    IAuthorizationPolicyProvider,
    ProveedorPoliticasPermisos
>();

builder.Services.AddControllersWithViews(options =>
{
    // APLICAR VALIDACIÓN DE CONTRASEÑA OBLIGATORIA
    // A TODOS LOS CONTROLADORES DEL SISTEMA.
    options.Filters.AddService<ForzarCambioContrasenaFiltro>();
});

// CONFIGURAR SERVICIO DE CORREO
builder.Services.Configure<ConfiguracionCorreo>(
    builder.Configuration.GetSection("Correo")
);

builder.Services.AddHttpContextAccessor();

//INYECIÓN DE DEPENDENCIAS DE SERVICIOS DEL SISTEMA
builder.Services.AddScoped<IServicioCorreo, ServicioCorreo>();
builder.Services.AddScoped<IUsuarioServicio, UsuarioServicio>();
builder.Services.AddScoped<IAutenticacionServicio, AutenticacionServicio>();
builder.Services.AddScoped<IRolServicio, RolServicio>();
builder.Services.AddScoped<IPermisoServicio, PermisoServicio>();

var app = builder.Build();

using (var ambito = app.Services.CreateScope())
{
    var servicios = ambito.ServiceProvider;

    await SeguridadSeeders.InicializarAsync(
        servicios,
        app.Configuration
    );
}

// CONFIGURAR CANAL DE SOLICITUDES HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // HSTS obliga al navegador a utilizar HTTPS en ambientes de producción.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// PERMITIR ARCHIVOS ESTÁTICOS SIN AUTENTICACIÓN
app.MapStaticAssets()
    .AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

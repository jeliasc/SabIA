using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Data;
using Proyecto_Final.Servicios.SeguridadAcademica;

namespace Proyecto_Final.Tests;

public sealed class AccesoAcademicoServicioTests
{
    [Fact]
    public async Task CuentaSinPerfilNiAmbitoInstitucional_NoObtieneAccesoPorDefecto()
    {
        var opciones = new DbContextOptionsBuilder<Contexto>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var contexto = new Contexto(opciones);
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "cuenta-sin-perfil")],
                "prueba"))
        };
        var servicio = new AccesoAcademicoServicio(
            contexto,
            new HttpContextAccessor { HttpContext = http });

        Assert.False(await servicio.PuedeConsultarAsignacionVigenteAsync(1));
        Assert.False(await servicio.PuedeConsultarAsignacionHistoricaAsync(1));
        Assert.False(await servicio.PuedeGestionarAsignacionVigenteAsync(1));
    }
}

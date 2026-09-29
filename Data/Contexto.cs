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

    protected override void OnModelCreating(
        ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<PermisoSistema>()
            .HasIndex(permiso => permiso.Codigo)
            .IsUnique();
    }
}
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Proyecto_Final.Models;

namespace Proyecto_Final.Data;

public class Contexto : IdentityDbContext<Usuario>
{
    public Contexto(DbContextOptions<Contexto> options)
        : base(options)
    {
    }
}
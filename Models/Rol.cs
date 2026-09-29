using Microsoft.AspNetCore.Identity;

namespace Proyecto_Final.Models;

public class Rol : IdentityRole
{
    public bool Activo { get; set; } = true;
}
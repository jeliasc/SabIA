using Microsoft.AspNetCore.Authorization;
using Proyecto_Final.Seguridad;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Permisos.Roles.Ver)]
public class RolesController : ModuloPrototipoController
{
    protected override string ClaveModulo => "roles";
}

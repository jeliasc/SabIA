using Microsoft.AspNetCore.Authorization;
using Proyecto_Final.Seguridad;

namespace Proyecto_Final.Controllers;

[Authorize(Policy = Permisos.PermisosSistema.Ver)]
public class PermisosSistemaController : ModuloPrototipoController
{
    protected override string ClaveModulo => "permisos";
}

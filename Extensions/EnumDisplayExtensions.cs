using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Proyecto_Final.Extensions;

/// <summary>
/// Obtiene el texto destinado a la interfaz sin alterar el valor interno del enum.
/// </summary>
public static class EnumDisplayExtensions
{
    public static string GetDisplayName(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        var display = member?.GetCustomAttribute<DisplayAttribute>();

        return display?.GetName() ?? value.ToString();
    }
}

using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Models.Shared.Extensions;

public static class EnumExtensions
{
    public static string ToValue(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<DisplayAttribute>();
        return attribute?.Name ?? value.ToString();
    }
}


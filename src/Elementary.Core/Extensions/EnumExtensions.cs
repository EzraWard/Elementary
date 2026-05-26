using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Elementary.Core.Extensions
{
    public static class EnumExtensions
    {        
        public static string GetDisplayName(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttribute<DisplayAttribute>();
            if (!string.IsNullOrWhiteSpace(attribute?.Name))
            {
                return attribute.Name;
            }

            var description = field?.GetCustomAttribute<DescriptionAttribute>();
            return description?.Description ?? value.ToString();
        }
    }
}

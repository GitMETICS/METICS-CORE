using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using webMetics.Models;

namespace webMetics.Helpers
{
    public static class EnumHelper
    {
        public static string GetDisplayName(Enum value)
        {
            var fieldInfo = value.GetType().GetField(value.ToString());
            var attribute = (DisplayAttribute)fieldInfo.GetCustomAttribute(typeof(DisplayAttribute));
            return attribute == null ? value.ToString() : attribute.Name;
        }

        public static TipoModalidad GetModalidadFromDisplayName(string displayName)
        {
            foreach (var field in typeof(TipoModalidad).GetFields())
            {
                var attribute = (DisplayAttribute)field.GetCustomAttribute(typeof(DisplayAttribute));
                if (attribute != null && attribute.Name == displayName)
                {
                    return (TipoModalidad)field.GetValue(null);
                }
            }
            throw new ArgumentException("Invalid display name", nameof(displayName));
        }
        public static string GetDisplayName(string enumValue)
        {
            if (string.IsNullOrEmpty(enumValue)) throw new ArgumentNullException(nameof(enumValue));

            var enumType = typeof(TipoModalidad); 
            if (!Enum.IsDefined(enumType, enumValue))
            {
                throw new ArgumentException($"Invalid enum value: {enumValue}", nameof(enumValue));
            }

            var value = (Enum)Enum.Parse(enumType, enumValue);

            return GetDisplayName(value);
        }
    }
}
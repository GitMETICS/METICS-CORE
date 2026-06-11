using System.ComponentModel.DataAnnotations;

namespace webMetics.Helpers
{
    /// <summary>
    /// Validador que asegura que el correo alternativo sea diferente del correo institucional
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class CorreoAlternativoDiferenteAttribute : ValidationAttribute
    {
        private readonly string _correoPropiedadNombre;

        public CorreoAlternativoDiferenteAttribute(string correoPropiedadNombre)
        {
            _correoPropiedadNombre = correoPropiedadNombre;
            ErrorMessage = "El correo alternativo debe ser diferente del correo institucional.";
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(value?.ToString()))
            {
                return ValidationResult.Success;
            }

            // Obtener la propiedad del correo institucional
            var correoProperty = validationContext.ObjectType.GetProperty(_correoPropiedadNombre);
            if (correoProperty == null)
            {
                return new ValidationResult($"Propiedad '{_correoPropiedadNombre}' no encontrada.");
            }

            var correoValue = correoProperty.GetValue(validationContext.ObjectInstance)?.ToString();

            // Si el correo institucional está vacío, pasar
            if (string.IsNullOrWhiteSpace(correoValue))
            {
                return ValidationResult.Success;
            }

            // Comparar sin case sensitivity
            if (correoValue.Equals(value.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }
    }
}

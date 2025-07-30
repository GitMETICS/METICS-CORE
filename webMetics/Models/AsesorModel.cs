using System.ComponentModel.DataAnnotations;

namespace webMetics.Models
{
    public class AsesorModel
    {
        public string? idAsesor { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un nombre.")]
        [Display(Name = "Nombre")]
        public required string nombre { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un apellido.")]
        [Display(Name = "Primer Apellido")]
        public required string primerApellido { get; set; }

        [Display(Name = "Segundo Apellido")]
        public string? segundoApellido { get; set; }

        [RegularExpression(@"[a-zA-Z0-9._%-]+@ucr\.ac\.cr", ErrorMessage = "El correo electrónico debe terminar con '@ucr.ac.cr'.")]
        [Required(ErrorMessage = "Es necesario ingresar un correo institucional.")]
        [Display(Name = "Correo Institucional")]
        public required string correo { get; set; }

        [Display(Name = "Tipo de Identificación")]
        public string? tipoIdentificacion { get; set; }

        [Display(Name = "Número de Identificación")]
        public string? numeroIdentificacion { get; set; }

        [Display(Name = "Descripción")]
        public string? descripcion { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un número de teléfono.")]
        [Display(Name = "Número de Teléfono")]
        public string? telefono { get; set; }

        // [Required(ErrorMessage = "Es necesario ingresar una contraseña temporal.")]
        [Display(Name = "Contraseña Temporal")]
        public string? contrasena { get; set; }

        // [Required(ErrorMessage = "Debe confirmar la contraseña temporal.")]
        [Display(Name = "Confirmar Contraseña")]
        public string? confirmarContrasena { get; set; }
    }
}
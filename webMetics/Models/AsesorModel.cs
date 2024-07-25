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
        [Display(Name = "Primer apellido")]
        public required string primerApellido { get; set; }

        [Display(Name = "Segundo apellido")]
        public string? segundoApellido { get; set; }

        [RegularExpression(@"[\w\.]+@ucr\.ac\.cr", ErrorMessage = "El correo electrónico debe terminar con '@ucr.ac.cr'.")]
        [Required(ErrorMessage = "Es necesario ingresar un correo institucional.")]
        [Display(Name = "Correo institucional")]
        public required string correo { get; set; }

        [Display(Name = "Tipo de identificación")]
        public string? tipoIdentificacion { get; set; }

        [Display(Name = "Número de identificación")]
        public string? numeroIdentificacion { get; set; }

        [Display(Name = "Descripción")]
        public string? descripcion { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un número de teléfono.")]
        [Display(Name = "Número de teléfono")]
        public string? telefono { get; set; }
    }
}
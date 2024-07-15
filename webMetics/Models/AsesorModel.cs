using System.ComponentModel.DataAnnotations;

namespace webMetics.Models
{
    public class AsesorModel
    {
        public required string idAsesor { get; set; }

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

        [Required(ErrorMessage = "Es necesario ingresar un tipo de identificación.")]
        [Display(Name = "Tipo de identificación")]
        public string? tipoIdentificacion { get; set; }

        [Display(Name = "Número de identificación")]
        public string? numeroIdentificacion { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un área.")]
        [Display(Name = "Área")]
        public string? area { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un departamento.")]
        [Display(Name = "Departamento")]
        public string? departamento { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar una unidad académica.")]
        [Display(Name = "Unidad académica")]
        public string? unidadAcademica { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar una sede.")]
        [Display(Name = "Sede")]
        public string? sede { get; set; }

        [Display(Name = "Descripción")]
        public string? descripcion { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar una condición actual.")]
        [Display(Name = "Condición actual")]
        public string? condicion { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un número de teléfono.")]
        [Display(Name = "Número de teléfono")]
        public string? telefono { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un tema asociado.")]
        [Display(Name = "Tema asociado")]
        public string? temaAsociado { get; set; }

        [Display(Name = "Asistente(s)")]
        public List<string>? asistentesAsociados { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;

namespace webMetics.Models
{
    public class UsuarioModel
    {
        public string? id { get; set; }

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

        [Required(ErrorMessage = "Es necesario ingresar el Departamento o Facultad.")]
        [Display(Name = "Facultad o Departamento")]
        public string? departamento { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar una Unidad Académica.")]
        [Display(Name = "Unidad académica")]
        public string? unidadAcademica { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un Recinto.")]
        [Display(Name = "Sede y Recinto")]
        public string? sede { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un tipo de participante.")]
        [Display(Name = "Tipo de participante")]
        public string? tipoParticipante { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar una condición actual.")]
        [Display(Name = "Condición actual en la institución")]
        public string? condicion { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un número de teléfono.")]
        [Display(Name = "Número de teléfono")]
        public string? telefono { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar una contraseña.")]
        [Display(Name = "Contraseña")]
        public required string contrasena {  get; set; }

        [Required(ErrorMessage = "Es necesario confirmar la contraseña.")]
        [Display(Name = "Confirmar contraseña")]
        public required string confirmarContrasena { get; set; }

        public ParticipanteModel? participante { get; set; }
    }

    public class LoginModel
    {
        // [RegularExpression(@"[\w\.]+@ucr\.ac\.cr", ErrorMessage = "El correo electrónico debe terminar con '@ucr.ac.cr'.")]
        [Required(ErrorMessage = "Es necesario ingresar un correo institucional.")]
        [Display(Name = "Correo institucional")]
        public string id { get; set; }

        [Display(Name = "Rol")]
        public int rol { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar una contraseña.")]
        [Display(Name = "Contraseña")]
        public string contrasena { get; set; }
    }

    public class NewLoginModel
    {
        // [RegularExpression(@"[\w\.]+@ucr\.ac\.cr", ErrorMessage = "El correo electrónico debe terminar con '@ucr.ac.cr'.")]
        [Required(ErrorMessage = "Es necesario ingresar un correo institucional.")]
        [Display(Name = "Correo institucional")]
        public string id { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar su contraseña actual.")]
        [Display(Name = "Contraseña actual")]
        public string contrasena { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar una contraseña.")]
        [Display(Name = "Nueva contraseña")]
        public string nuevaContrasena { get; set; }

        [Required(ErrorMessage = "Es necesario confirmar la nueva contraseña.")]
        [Display(Name = "Confirmar nueva contraseña")]
        public string confirmarContrasena { get; set; }
    }
}
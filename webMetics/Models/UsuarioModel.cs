using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace webMetics.Models
{
    public class UsuarioModel
    {
        [Required(ErrorMessage = "Es necesario ingresar un número de identificación.")]
        [Display(Name = "Número de identificación")]
        public string identificacion { get; set; }

        [RegularExpression(@"[\w\.]+@ucr\.ac\.cr", ErrorMessage = "El correo electrónico debe terminar con '@ucr.ac.cr'.")]
        [Required(ErrorMessage = "Es necesario ingresar un correo institucional.")]
        [Display(Name = "Correo institucional")]
        public string correo { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar una contraseña.")]
        [Display(Name = "Contraseña")]
        public string contrasena {  get; set; }

        [Required(ErrorMessage = "Es necesario confirmar la contraseña.")]
        [Display(Name = "Confirmar contraseña")]
        public string confirmarContrasena { get; set; }

        public ParticipanteModel? participante { get; set; }
    }

    public class LoginModel
    {
        [Required(ErrorMessage = "Es necesario ingresar una identificación.")]
        [Display(Name = "Identificación")]
        public string identificacion { get; set; }

        [Display(Name = "Rol")]
        public int rol { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar una contraseña.")]
        [Display(Name = "Contraseña")]
        public string contrasena { get; set; }
    }

    public class NewLoginModel
    {
        [Display(Name = "Identificación")]
        public string identificacion { get; set; }

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
using System;
using System.ComponentModel.DataAnnotations;


namespace webMetics.Models
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Es necesario ingresar un número de identificación.")]
        [Display(Name = "Número de identificación")]
        public string identificacion { get; set; }

        [Display(Name = "Rol")]
        public int rol { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar una contraseña.")]
        [Display(Name = "Contraseña")]
        public string contrasena { get; set; }
    }

    public class NewLoginModel
    {
        [Display(Name = "Número de identificación")]
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
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

        [Required(ErrorMessage = "Es necesario ingresar un correo institucional de la Universidad de Costa Rica.")]
        [Display(Name = "Correo institucional")]
        public string correo { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar una contraseña.")]
        [Display(Name = "Contraseña")]
        public string contrasena {  get; set; }
    }
}
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace webMetics.Models
{
    public class AsesorModel
    {
        [Required(ErrorMessage = "Es necesario ingresar un identificador de usuario.")]
        [Display(Name = "Número de identificación")]
        public string identificacion { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un nombre.")]
        [Display(Name = "Nombre")]
        public string nombre { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un apellido.")]
        [Display(Name = "Primer apellido")]
        public string apellido1 { get; set; }

        [Display(Name = "Segundo apellido")]
        public string apellido2 { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un tema asociado.")]
        [Display(Name = "Tema asociado")]
        public string temaAsociado { get; set; }

        [Display(Name = "Asistente(s)")]
        public List<string> asistentesAsociados { get; set; }

        [Display(Name = "Descripción")]
        public string descripcion { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar al menos un número telefónico.")]
        [RegularExpression("^[0-9]*$", ErrorMessage = "Ingrese un número de teléfono válido.")]
        [Display(Name = "Teléfonos")]
        public string telefonos { get; set; }
    }
}
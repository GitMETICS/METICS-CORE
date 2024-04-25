using System.ComponentModel.DataAnnotations;
using System;

namespace webMetics.Models
{
    public class TipoActividadModel
    {
        [Required(ErrorMessage = "Es necesario ingresar un nombre para el tipo de actividad.")]
        [Display(Name = "Nombre")]
        public string nombre { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar una descripción para el tipo de actividad.")]
        [Display(Name = "Descripción")]
        public string descripcion { get; set; }

        public string idGenerado { get; set; }
    }
}
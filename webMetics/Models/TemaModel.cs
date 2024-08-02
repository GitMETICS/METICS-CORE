using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webMetics.Models
{
    public class TemaModel
    {
        [Required(ErrorMessage = "Es necesario ingresar una categoría.")]
        [Display(Name = "Categoría")]
        public string categoria { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un tipo de actividad.")]
        [Display(Name = "Tipo de actividad")]
        public string tipoActividad { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un nombre.")]
        [Display(Name = "Nombre")]
        public string nombre { get; set; }

        public int idTema { get; set; }
    }
}
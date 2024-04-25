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

        [Required(ErrorMessage = "Es necesario ingresar un asesor principal.")]
        [Display(Name = "Identificación de asesor principal")]
        public string idAsesorPrincipal { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un asesor principal.")]
        [Display(Name = "Asesor principal")]
        public string asesorPrincipal { get; set; }

        [Display(Name = "Asesores de apoyo")]
        [NotMapped]
        public IEnumerable<string> asesoresApoyo { get; set; }

        public string asesores;

        public int idTema { get; set; }
    }
}
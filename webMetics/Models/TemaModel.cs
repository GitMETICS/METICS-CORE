using System.ComponentModel.DataAnnotations;

namespace webMetics.Models
{
    public class TemaModel
    {
        public int idTema { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un nombre para el área de competencia.")]
        [Display(Name = "Nombre")]
        public string? nombre { get; set; }

        /* [Required(ErrorMessage = "Es necesario ingresar un tipo de actividad.")]
        [Display(Name = "Tipo de actividad")]
        public string tipoActividad { get; set; } */
    }
}
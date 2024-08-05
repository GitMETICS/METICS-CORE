using System.ComponentModel.DataAnnotations;

namespace webMetics.Models
{
    public class CategoriaModel
    {
        public int idCategoria { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un nombre para la categoría.")]
        [Display(Name = "Nombre")]
        public string? nombre { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar una descripción para la categoría.")]
        [Display(Name = "Descripción")]
        public string? descripcion { get; set; }
    }

    public class TemaModel
    {
        public int idTema { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un nombre.")]
        [Display(Name = "Nombre")]
        public string? nombre { get; set; }

        /* [Required(ErrorMessage = "Es necesario ingresar un tipo de actividad.")]
        [Display(Name = "Tipo de actividad")]
        public string tipoActividad { get; set; } */
    }
}
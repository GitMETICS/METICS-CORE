using System.ComponentModel.DataAnnotations;

namespace webMetics.Models
{
    public class CategoriaModel
    {
        public int idCategoria { get; set; }

        [Required(ErrorMessage = "Es necesario definir el nivel.")]
        [Display(Name = "Nombre")]
        public string? nombre { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar una descripción para el nivel.")]
        [Display(Name = "Descripción")]
        public string? descripcion { get; set; }
    }
}
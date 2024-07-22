using System;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace webMetics.Models
{
    public class GrupoModel
    {
        public int idGrupo { get; set; }

        [Required(ErrorMessage = "Es necesario que ingrese el nombre del módulo.")]
        [Display(Name = "Nombre del módulo")]
        public string nombre { get; set; }

        [Required(ErrorMessage = "Agregue la descripción del módulo.")]
        [Display(Name = "Descripción")]
        public string descripcion { get; set; }

        [Required(ErrorMessage = "Es necesario seleccionar una modalidad.")]
        [Display(Name = "Modalidad")]
        public string modalidad { get; set; }

        [Required(ErrorMessage = "Ingrese la cantidad de horas.")]
        [Display(Name = "Cantidad de horas")]
        [Range(1, 100, ErrorMessage = "La cantidad de horas debe ser un número entre 1 y 100.")]
        public int cantidadHoras { get; set; }

        [Required(ErrorMessage = "Ingrese el cupo.")]
        [Display(Name = "Cupo")]
        [Range(1, 50, ErrorMessage = "El cupo debe ser un número entre 1 y 50.")]
        public int cupo { get; set; }

        [Display(Name = "Cupo actual")]
        public int cupoActual { get; set; }

        [Required(ErrorMessage = "Agregue el lugar o enlace donde se imparte el curso.")]
        [Display(Name = "Lugar")]
        public string lugar { get; set; }

        [Required(ErrorMessage = "Es necesario que ingrese el horario del módulo.")]
        [Display(Name = "Horario")]
        public string horario { get; set; }

        [Required(ErrorMessage = "Debe asociar un tema al módulo.")]
        [Display(Name = "Tema asociado")]
        public string? temaAsociado { get; set; }

        [Required(ErrorMessage = "Es necesario que seleccione la fecha de inicio de la actividad.")]
        [Display(Name = "Fecha de inicio")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime fechaInicioGrupo { get; set; }

        [Required(ErrorMessage = "Es necesario que seleccione la fecha de finalización de la actividad.")]
        [Display(Name = "Fecha de finalización")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime fechaFinalizacionGrupo { get; set; }

        [Required(ErrorMessage = "Es necesario que seleccione la fecha de inicio de la inscripción.")]
        [Display(Name = "Fecha de inicio de la inscripción")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime fechaInicioInscripcion { get; set; }

        [Required(ErrorMessage = "Es necesario que seleccione la fecha de finalización de la inscripción.")]
        [Display(Name = "Fecha de finalización de la inscripción")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime fechaFinalizacionInscripcion { get; set; }

        public bool esVisible { get; set; }

        public string? nombreArchivo { get; set; }

        // [Required(ErrorMessage = "Debe adjuntar el documento del programa del módulo.")]
        public IFormFile? archivoAdjunto { get; set; }

        [Display(Name = "Asesor")]
        public string? nombreAsesorAsociado { get; set; }

        public string? tipoActividadAsociado { get; set; } // TODO: Estos nullable types se deben revisar según requerimientos

    }

    public enum TipoModalidad
    {
        Autogestionado,
        Presencial,
        BajoVirtual,
        Bimodal,
        AltoVirtual
    }
}
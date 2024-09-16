using System;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace webMetics.Models
{
    public class GrupoModel
    {
        public int idGrupo { get; set; }

        [Required(ErrorMessage = "Debe asociar un área de competencia al módulo.")]
        public int idTema { get; set; }
        
        [Display(Name = "Área de competencia")]
        public string? nombreTema { get; set; }

        [Required(ErrorMessage = "Debe asociar un nivel al módulo.")]
        public int idCategoria { get; set; }

        [Display(Name = "Nivel")]
        public string? nombreCategoria { get; set; }

        [Required(ErrorMessage = "Es necesario que ingrese el nombre del módulo.")]
        [Display(Name = "Nombre del módulo")]
        public required string nombre { get; set; }

        [Required(ErrorMessage = "Es necesario que ingrese un número de grupo.")]
        [Display(Name = "Número de grupo")]
        public required int numeroGrupo { get; set; }

        [Display(Name = "Descripción")]
        public string? descripcion { get; set; }

        [Required(ErrorMessage = "Es necesario seleccionar una modalidad.")]
        [Display(Name = "Modalidad")]
        public required string modalidad { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar la cantidad de horas del módulo.")]
        [Display(Name = "Cantidad de horas")]
        [Range(1, 100, ErrorMessage = "La cantidad de horas debe ser un número entre 1 y 100.")]
        public int cantidadHoras { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar el cupo del módulo.")]
        [Display(Name = "Cupo")]
        [Range(1, 50, ErrorMessage = "El cupo debe ser un número entre 1 y 50.")]
        public int cupo { get; set; }

        [Display(Name = "Cupo actual")]
        public int cupoActual { get; set; }

        // [Required(ErrorMessage = "Agregue el lugar o enlace donde se imparte el curso.")]
        [Display(Name = "Lugar")]
        public string? lugar { get; set; }

        // [Required(ErrorMessage = "Introduzca el horario del módulo.")]
        [Display(Name = "Horario")]
        public string? horario { get; set; }

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

        [Display(Name = "Área(s) de competencia")]
        public List<int>? TemasSeleccionados { get; set; }

        public List<string>? TemasSeleccionadosNombres { get; set; }


        public bool esVisible { get; set; }

        public string? nombreArchivo { get; set; }

        public IFormFile? archivoAdjunto { get; set; }

        // [Required(ErrorMessage = "Es necesario elegir un asesor del módulo.")]
        public string? idAsesor { get; set; }

        [Display(Name = "Facilitador(a)")]
        public string? nombreAsesor { get; set; }
    }

    public enum TipoModalidad
    {
        [Display(Name = "Autogestionado")]
        Autogestionado,

        [Display(Name = "Presencial")]
        Presencial,

        [Display(Name = "Bajo Virtual")]
        BajoVirtual,

        [Display(Name = "Bimodal")]
        Bimodal,

        [Display(Name = "Alto Virtual")]
        AltoVirtual,

        [Display(Name = "Virtual")]
        Virtual
    }
}
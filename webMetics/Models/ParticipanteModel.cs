using System.ComponentModel.DataAnnotations;

namespace webMetics.Models
{
    public class ParticipanteModel
    {
        public string? idParticipante { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un nombre.")]
        [Display(Name = "Nombre")]
        public required string nombre { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un apellido.")]
        [Display(Name = "Primer apellido")]
        public required string primerApellido { get; set; }

        [Display(Name = "Segundo apellido")]
        public string? segundoApellido { get; set; }

        [RegularExpression(@"[a-zA-Z0-9._%-]+@ucr\.ac\.cr", ErrorMessage = "El correo electrónico debe terminar con '@ucr.ac.cr'.")]
        [Required(ErrorMessage = "Es necesario ingresar un correo institucional.")]
        [Display(Name = "Correo institucional")]
        public required string correo { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un tipo de identificación.")]
        [Display(Name = "Tipo de identificación")]
        public string? tipoIdentificacion { get; set; }

        [RegularExpression(@"^((\d-\d{4}-\d{4})|(\d{9}))$", ErrorMessage = "Debe seguir alguno de los siguientes formatos: 0-0000-0000 o 123456789.")]
        [Display(Name = "Número de identificación")]
        public string? numeroIdentificacion { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar el Área Académica.")]
        [Display(Name = "Área")]
        public string? area { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar una Facultad o Departamento.")]
        [Display(Name = "Facultad o Departamento")]
        public string? departamento { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar una Unidad Académica.")]
        [Display(Name = "Unidad académica")]
        public string? unidadAcademica { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar una Sede o Recinto.")]
        [Display(Name = "Sede y Recinto")]
        public string? sede { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un tipo de participante.")]
        [Display(Name = "Tipo de participante")]
        public string? tipoParticipante { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar una condición actual en la Institución.")]
        [Display(Name = "Condición actual en la Institución")]
        public string? condicion { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un número de teléfono.")]
        [Display(Name = "Número de teléfono")]
        public string? telefono { get; set; }

        [Display(Name = "Horas matriculadas")]
        [Range(0, int.MaxValue, ErrorMessage = "Las horas matriculadas deben ser al menos 0.")]
        public int horasMatriculadas { get; set; }

        [Display(Name = "Horas aprobadas")]
        [Range(0, int.MaxValue, ErrorMessage = "Las horas aprobadas deben ser al menos 0.")]
        public int horasAprobadas { get; set; }

        public int correoNotificacionEnviado { get; set; }

        public List<GrupoModel>? gruposInscritos { get; set; }
    }
    public enum TipoIdentificacion
    {
        Cédula,
        Residente,
        Pasaporte
    }

    public enum TipoDeParticipantes
    {
        [Display(Name = "Profesor(a)")]
        Profesor,

        [Display(Name = "Director(a)")]
        Director,

        [Display(Name = "Asistente")]
        Asistente
    }
}
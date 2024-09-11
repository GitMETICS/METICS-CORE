using System.ComponentModel.DataAnnotations;

namespace webMetics.Models
{
    public class InscripcionModel
    {
        public int idInscripcion { get; set; }

        public required int idGrupo { get; set; }

        public required string idParticipante { get; set; }

        public int numeroGrupo { get; set; }

        public string? nombreGrupo { get; set; }

        public string? estado { get; set; }

        public string? observaciones { get; set; }

        public int horasAprobadas { get; set; }

        public int horasMatriculadas { get; set; }

        public double calificacion {  get; set; }

        [RegularExpression(@"[\w\.]+@ucr\.ac\.cr", ErrorMessage = "El correo electrónico debe terminar con '@ucr.ac.cr'.")]
        [Required(ErrorMessage = "Es necesario ingresar un correo institucional.")]
        [Display(Name = "Correo de Notificación sobre Horas Inscritas")]
        public string? correoLimiteHoras { get; set; }
    }
}
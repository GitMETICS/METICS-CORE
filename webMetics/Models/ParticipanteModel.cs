using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace webMetics.Models
{
    public class ParticipanteModel
    {
        [Display(Name = "Número de identificación")]
        public string idParticipante { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un nombre.")]
        [Display(Name = "Nombre")]
        public string nombre { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un apellido.")]
        [Display(Name = "Primer apellido")]
        public string apellido_1 { get; set; }

        [Display(Name = "Segundo apellido")]
        public string apellido_2 { get; set; }

        [RegularExpression(@"[\w\.]+@ucr\.ac\.cr", ErrorMessage = "El correo electrónico debe terminar con '@ucr.ac.cr'.")]
        [Required(ErrorMessage = "Es necesario ingresar un correo institucional.")]
        [Display(Name = "Correo institucional")]
        public string correo { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un tipo de identificación.")]
        [Display(Name = "Tipo de identificación")]
        public string tipoIdentificacion { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un tipo de participante.")]
        [Display(Name = "Tipo de participante")]
        public string tipoParticipante { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar una unidad académica.")]
        [Display(Name = "Unidad académica")]
        public string unidadAcademica { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un área.")]
        [Display(Name = "Área a la que pertenece")]
        public string area { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un departamento.")]
        [Display(Name = "Departamento al que pertenece")]
        public string departamento { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar una sección.")]
        [Display(Name = "Sección a la que pertenece")]
        public string seccion { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar una condición actual.")]
        [Display(Name = "Condición actual")]
        public string condicion { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un número de teléfono.")]
        [Display(Name = "Número de teléfono")]
        public string telefonos { get; set; }

        [Display(Name = "Horas matriculadas")]
        public int horasMatriculadas { get; set; }

        [Display(Name = "Horas aprobadas")]
        public int horasAprobadas { get; set; }

        public List<GrupoModel> gruposInscritos { get; set; }

        public int rol;
    }
    public enum TipoIdentificacion
    {
        Cédula,
        Residente,
        Pasaporte
    }

    public enum TipoDeParticipantes
    {
        Profesor,
        Director,
        Asistente
    }
}
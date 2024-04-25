using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace webMetics.Models
{
    public class UsuarioModel
    {
        [Required(ErrorMessage = "Es necesario ingresar un número de identificación.")]
        [Display(Name = "Número de identificación")]
        public string identificacion { get; set; }

        public ParticipanteModel participante { get; set; }
    }
}
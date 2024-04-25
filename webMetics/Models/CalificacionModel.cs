using System;
using System.ComponentModel.DataAnnotations;

namespace webMetics.Models
{
    public class CalificacionModel
    {
        public int idGrupo { get; set; }

        public ParticipanteModel participante { get; set; }

        public double calificacion { get; set; }
    }
}
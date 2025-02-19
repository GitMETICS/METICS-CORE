using System;
using System.ComponentModel.DataAnnotations;

namespace webMetics.Models
{
    public class CalificacionModel
    {
        public int idGrupo { get; set; }

        public ParticipanteModel participante { get; set; }

        [Range(0, 100, ErrorMessage = "Debe ser un número entre 0 y 100.")]
        public double calificacion { get; set; }

        public int horasAprobadas { get; set; }

        required public string estado { get; set; }

    }
}
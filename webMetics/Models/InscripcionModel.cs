﻿using System.ComponentModel.DataAnnotations;

namespace webMetics.Models
{
    public class InscripcionModel
    {
        public int idInscripcion { get; set; }

        public int idGrupo { get; set; }

        public required string idParticipante { get; set; }

        public int numeroGrupo { get; set; }

        public string? nombreGrupo { get; set; }

        public string? estado { get; set; }

        public string? observaciones { get; set; }

        public int horasAprobadas { get; set; }

        public int horasMatriculadas { get; set; }

        public double calificacion {  get; set; }

        public ParticipanteModel? participante { get; set; }
    }
}
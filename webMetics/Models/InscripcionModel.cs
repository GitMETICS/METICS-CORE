using System;

namespace webMetics.Models
{
    public class InscripcionModel
    {

        public int idInscripcion { get; set; }

        public int idGrupo { get; set; }

        public string idParticipante { get; set; }

        public string estado { get; set; }

        public string observaciones { get; set; }

        public string participanteAsociado { get; set; }

    }
}
using System;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace webMetics.Models
{
    public class ExportarCalificacionesModel
    {
        public string nombreGrupo { get; set; }

        public string nombreAsesorAsociado { get; set; }

        public List<CalificacionModel> listaCalificaciones { get; set; }
    }

    public class ExportarParticipantesModel
    {
        public string nombreGrupo { get; set; }

        public string nombreAsesorAsociado { get; set; }

        public List<ParticipanteModel> listaParticipantes { get; set; }
    }
}
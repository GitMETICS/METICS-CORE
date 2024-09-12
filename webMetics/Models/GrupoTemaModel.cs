using System;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace webMetics.Models
{
    public class GrupoTemaModel
    {
        public required int idGrupo { get; set; }

        public required int idTema { get; set; }
    }
}
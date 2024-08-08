namespace webMetics.Models
{
    public class InscripcionModel
    {
        public int idInscripcion { get; set; }

        public required int idGrupo { get; set; }

        public required string idParticipante { get; set; }

        public string? estado { get; set; }

        public string? observaciones { get; set; }

        public int horasAprobadas { get; set; }

        public int horasMatriculadas { get; set; }
    }
}
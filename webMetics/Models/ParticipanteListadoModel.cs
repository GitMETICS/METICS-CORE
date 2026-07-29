namespace webMetics.Models
{
    public class ParticipanteListadoModel
    {
        public required string IdParticipante { get; set; }

        public required string Nombre { get; set; }

        public required string PrimerApellido { get; set; }

        public string? SegundoApellido { get; set; }

        public required string Correo { get; set; }

        public string? UnidadAcademica { get; set; }

        public int HorasMatriculadas { get; set; }

        public int HorasAprobadas { get; set; }

        public int CorreoNotificacionEnviado { get; set; }
    }
}

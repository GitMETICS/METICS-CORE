namespace webMetics.Services.Sso
{
    /// <summary>
    /// Datos de identidad extraídos de los claims del IdP que se usan para
    /// aprovisionar (JIT) un usuario/participante nuevo en la primera sesión.
    /// </summary>
    public class SsoUserInfo
    {
        public string Email { get; init; } = "";
        public string Nombre { get; init; } = "";
        public string PrimerApellido { get; init; } = "";
        public string? SegundoApellido { get; init; }
        public string? TipoIdentificacion { get; init; }
        public string? NumeroIdentificacion { get; init; }
    }
}

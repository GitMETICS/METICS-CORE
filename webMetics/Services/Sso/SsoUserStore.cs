using webMetics.Handlers;
using webMetics.Models;

namespace webMetics.Services.Sso
{
    /// <summary>
    /// Adaptador de <see cref="ISsoUserStore"/> sobre los handlers existentes.
    /// Mantiene la misma convención del resto de la app: los handlers se instancian
    /// con <c>new</c> (no hay DI) y cada uno gestiona su propia conexión.
    /// </summary>
    public class SsoUserStore : ISsoUserStore
    {
        private readonly UsuarioHandler _usuarios;
        private readonly ParticipanteHandler _participantes;
        private readonly AsesorHandler _asesores;

        public SsoUserStore(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _usuarios = new UsuarioHandler(environment, configuration);
            _participantes = new ParticipanteHandler(environment, configuration);
            _asesores = new AsesorHandler(environment, configuration);
        }

        public bool ExisteUsuario(string email) => _usuarios.ExisteUsuario(email);

        public LoginModel? ObtenerUsuario(string email) => _usuarios.ObtenerUsuario(email);

        public bool ExisteAsesor(string email) => _asesores.ExisteAsesor(email);

        public void RegistrarAcceso(string email, string estado) =>
            _usuarios.InsertarAccesoUsuarioBitacora(email, estado);

        public void Provisionar(SsoUserInfo info, int rol)
        {
            // Los usuarios de SSO se autentican en el IdP: nunca usan esta contraseña,
            // pero el esquema la requiere, así que se genera una aleatoria e inutilizable.
            if (!_usuarios.ExisteUsuario(info.Email))
            {
                _usuarios.CrearUsuario(info.Email, GenerarContrasenaAleatoria(), rol);
            }

            if (!_participantes.ExisteParticipante(info.Email))
            {
                _participantes.CrearParticipante(new ParticipanteModel
                {
                    idParticipante = info.Email,
                    nombre = string.IsNullOrWhiteSpace(info.Nombre) ? info.Email : info.Nombre,
                    primerApellido = info.PrimerApellido ?? string.Empty,
                    segundoApellido = info.SegundoApellido,
                    correo = info.Email,
                    tipoIdentificacion = info.TipoIdentificacion,
                    numeroIdentificacion = info.NumeroIdentificacion,
                    horasMatriculadas = 0,
                    horasAprobadas = 0
                });
            }

            // El usuario queda plenamente registrado tras la primera sesión por SSO,
            // igual que el auto-registro por contraseña marca este indicador.
            _usuarios.ActualizarRegistradoPorUsuario(info.Email);
        }

        private static string GenerarContrasenaAleatoria()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 16).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}

using webMetics.Models;

namespace webMetics.Services.Sso
{
    /// <summary>
    /// "Interfaz reducida sobre los handlers de datos que necesita el flujo de SSO.
    /// Permite probar <see cref="UcrSsoService"/> sin una base de datos real.
    /// </summary>
    public interface ISsoUserStore
    {
        /// <summary>¿Existe ya un usuario con este correo institucional?</summary>
        bool ExisteUsuario(string email);

        /// <summary>Obtiene el usuario (id + rol) o <c>null</c> si no existe.</summary>
        LoginModel? ObtenerUsuario(string email);

        /// <summary>¿Fue pre-registrado como asesor por un administrador? (determina la promoción a rol 2).</summary>
        bool ExisteAsesor(string email);

        /// <summary>Crea el usuario y su participante asociado (JIT) con el rol indicado.</summary>
        void Provisionar(SsoUserInfo info, int rol);

        /// <summary>Registra el intento de acceso en la bitácora ("EXITO" / "FRACASO").</summary>
        void RegistrarAcceso(string email, string estado);
    }
}

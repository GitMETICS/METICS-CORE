namespace webMetics.Services.Sso
{
    /// <summary>
    /// Resultado puro de resolver un inicio de sesión por SSO: a quién corresponde,
    /// con qué rol, si se aprovisionó en el momento, o el motivo de rechazo.
    /// No realiza ninguna operación HTTP ni de cookies.
    /// </summary>
    public class SsoLoginResult
    {
        public bool Success { get; init; }

        /// <summary>Identificador del usuario (= correo institucional) cuando <see cref="Success"/> es verdadero.</summary>
        public string? Id { get; init; }

        /// <summary>Rol de la aplicación: 0 = participante, 1 = administrador, 2 = asesor.</summary>
        public int Rol { get; init; }

        /// <summary>Verdadero si el usuario fue creado (JIT) durante este inicio de sesión.</summary>
        public bool Provisioned { get; init; }

        /// <summary>Mensaje mostrable al usuario cuando el inicio de sesión es rechazado.</summary>
        public string? ErrorMessage { get; init; }

        /// <summary>Duración de la cookie de sesión: administradores 120 minutos, resto 20.</summary>
        public int CookieMinutes => Rol == 1 ? 120 : 20;

        public static SsoLoginResult Rejected(string message) =>
            new() { Success = false, ErrorMessage = message };

        public static SsoLoginResult Authenticated(string id, int rol, bool provisioned) =>
            new() { Success = true, Id = id, Rol = rol, Provisioned = provisioned };
    }
}

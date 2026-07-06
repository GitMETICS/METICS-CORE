using System.Security.Claims;

namespace webMetics.Services.Sso
{
    /// <summary>
    /// Lógica pura del inicio de sesión por SSO de la UCR: extrae y valida el correo
    /// institucional de los claims, busca o aprovisiona (JIT) al usuario y decide el rol.
    /// No escribe cookies ni toca <c>HttpContext</c>; todo el acceso a datos pasa por
    /// <see cref="ISsoUserStore"/>, lo que la hace unitariamente comprobable.
    /// </summary>
    public class UcrSsoService
    {
        private const string DominioUcr = "@ucr.ac.cr";

        private readonly ISsoUserStore _store;
        private readonly UcrSsoOptions _options;

        public UcrSsoService(ISsoUserStore store, UcrSsoOptions? options = null)
        {
            _store = store;
            _options = options ?? new UcrSsoOptions();
        }

        /// <summary>
        /// Resuelve el inicio de sesión a partir del principal autenticado por el IdP.
        /// Usuarios existentes conservan su rol; usuarios nuevos se crean con rol 0
        /// (o 2 si fueron pre-registrados como asesores por un administrador).
        /// </summary>
        public SsoLoginResult ResolveLogin(ClaimsPrincipal principal)
        {
            string? email = ExtractEmail(principal);

            if (string.IsNullOrWhiteSpace(email))
            {
                return SsoLoginResult.Rejected(
                    "No se recibió un correo institucional del proveedor de identidad de la UCR.");
            }

            email = email.Trim().ToLowerInvariant();

            if (!EsCorreoInstitucional(email))
            {
                return SsoLoginResult.Rejected(
                    "El inicio de sesión con UCR requiere un correo institucional @ucr.ac.cr.");
            }

            if (_store.ExisteUsuario(email))
            {
                int rolExistente = _store.ObtenerUsuario(email)?.rol ?? 0;
                return SsoLoginResult.Authenticated(email, rolExistente, provisioned: false);
            }

            int rolNuevo = _store.ExisteAsesor(email) ? 2 : 0;
            _store.Provisionar(BuildUserInfo(principal, email), rolNuevo);
            return SsoLoginResult.Authenticated(email, rolNuevo, provisioned: true);
        }

        /// <summary>Extrae el correo institucional del primer claim configurado que esté presente.</summary>
        public string? ExtractEmail(ClaimsPrincipal principal)
        {
            if (principal == null)
            {
                return null;
            }

            foreach (string claimType in _options.EmailClaimTypes)
            {
                string? value = principal.FindFirst(claimType)?.Value;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
        }

        private static bool EsCorreoInstitucional(string email)
        {
            return email.EndsWith(DominioUcr, StringComparison.OrdinalIgnoreCase)
                && email.Length > DominioUcr.Length;
        }

        private SsoUserInfo BuildUserInfo(ClaimsPrincipal principal, string email)
        {
            return new SsoUserInfo
            {
                Email = email,
                Nombre = principal.FindFirst(_options.NombreClaim)?.Value ?? string.Empty,
                PrimerApellido = principal.FindFirst(_options.PrimerApellidoClaim)?.Value ?? string.Empty,
                SegundoApellido = principal.FindFirst(_options.SegundoApellidoClaim)?.Value,
                NumeroIdentificacion = principal.FindFirst(_options.NumeroIdentificacionClaim)?.Value,
                TipoIdentificacion = _options.TipoIdentificacion
            };
        }

    }
}

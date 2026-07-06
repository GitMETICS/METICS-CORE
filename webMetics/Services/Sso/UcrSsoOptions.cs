using System.Security.Claims;

namespace webMetics.Services.Sso
{
    /// <summary>
    /// Configuración del inicio de sesión único (OIDC) con la plataforma de
    /// autenticación única de la UCR. Se enlaza desde la sección
    /// <c>Authentication:UCR</c> de la configuración.
    /// </summary>
    public class UcrSsoOptions
    {
        /// <summary>URL del proveedor de identidad (idealmente con documento de descubrimiento OIDC).</summary>
        public string? Authority { get; set; }

        /// <summary>Identificador de cliente registrado ante el Centro de Informática.</summary>
        public string? ClientId { get; set; }

        /// <summary>Secreto de cliente. Debe provenir de user-secrets o variables de entorno, no del appsettings versionado.</summary>
        public string? ClientSecret { get; set; }

        /// <summary>Ruta del callback de autenticación (redirect URI registrado en el IdP).</summary>
        public string CallbackPath { get; set; } = "/signin-oidc";

        /// <summary>Ruta del callback de cierre de sesión federado.</summary>
        public string SignedOutCallbackPath { get; set; } = "/signout-callback-oidc";

        /// <summary>
        /// Exigir HTTPS para el documento de metadatos del IdP. Debe ser <c>true</c> en producción;
        /// se pone en <c>false</c> solo en desarrollo cuando se apunta a un IdP simulado por HTTP.
        /// </summary>
        public bool RequireHttpsMetadata { get; set; } = true;

        /// <summary>Scopes solicitados al IdP.</summary>
        public List<string> Scopes { get; set; } = new() { "openid", "profile", "email" };

        /// <summary>
        /// Tipos de claim (en orden de preferencia) de los que se extrae el correo institucional.
        /// El IdP de la UCR expone el correo institucional como <c>mail</c>; se incluyen alternativas estándar.
        /// </summary>
        public List<string> EmailClaimTypes { get; set; } = new()
        {
            "email", "mail", "preferred_username", "upn", ClaimTypes.Email, "unique_name"
        };

        public string NombreClaim { get; set; } = "given_name";
        public string PrimerApellidoClaim { get; set; } = "family_name";
        public string SegundoApellidoClaim { get; set; } = "ucrMotherSn";
        public string NumeroIdentificacionClaim { get; set; } = "ucrUserId";
        public string TipoIdentificacion { get; set; } = "Cédula";

        /// <summary>El SSO solo está habilitado cuando hay authority y client id configurados.</summary>
        public bool Enabled => !string.IsNullOrWhiteSpace(Authority) && !string.IsNullOrWhiteSpace(ClientId);
    }
}

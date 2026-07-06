using System.Security.Claims;
using FluentAssertions;
using Moq;
using webMetics.Models;
using webMetics.Services.Sso;
using Xunit;

namespace webMetics.Tests
{
    /// <summary>
    /// Cobertura del núcleo del inicio de sesión por SSO: mapeo de claims → usuario,
    /// aprovisionamiento JIT, promoción de asesores a rol 2 y validaciones de dominio.
    /// Usa un <see cref="ISsoUserStore"/> simulado, así que no toca base de datos ni IdP.
    /// </summary>
    public class UcrSsoServiceTests
    {
        private readonly Mock<ISsoUserStore> _store = new(MockBehavior.Strict);

        private UcrSsoService CrearServicio(UcrSsoOptions? options = null) =>
            new(_store.Object, options);

        private static ClaimsPrincipal PrincipalConCorreo(string? email, params (string Type, string Value)[] extra)
        {
            var claims = new List<Claim>();
            if (email != null)
            {
                claims.Add(new Claim("email", email));
            }
            foreach (var (type, value) in extra)
            {
                claims.Add(new Claim(type, value));
            }
            return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "oidc"));
        }

        [Fact]
        public void ExistingUser_PreservesRole_AndDoesNotProvision()
        {
            _store.Setup(s => s.ExisteUsuario("ana.mora@ucr.ac.cr")).Returns(true);
            _store.Setup(s => s.ObtenerUsuario("ana.mora@ucr.ac.cr"))
                  .Returns(new LoginModel { id = "ana.mora@ucr.ac.cr", rol = 1, contrasena = "x" });

            var result = CrearServicio().ResolveLogin(PrincipalConCorreo("ana.mora@ucr.ac.cr"));

            result.Success.Should().BeTrue();
            result.Id.Should().Be("ana.mora@ucr.ac.cr");
            result.Rol.Should().Be(1);
            result.Provisioned.Should().BeFalse();
            result.CookieMinutes.Should().Be(120); // admin
            _store.Verify(s => s.Provisionar(It.IsAny<SsoUserInfo>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void NewInstitutionalUser_IsProvisioned_WithRoleZero()
        {
            _store.Setup(s => s.ExisteUsuario("nuevo@ucr.ac.cr")).Returns(false);
            _store.Setup(s => s.ExisteAsesor("nuevo@ucr.ac.cr")).Returns(false);
            _store.Setup(s => s.Provisionar(It.IsAny<SsoUserInfo>(), 0));

            var result = CrearServicio().ResolveLogin(PrincipalConCorreo("nuevo@ucr.ac.cr"));

            result.Success.Should().BeTrue();
            result.Rol.Should().Be(0);
            result.Provisioned.Should().BeTrue();
            result.CookieMinutes.Should().Be(20); // no admin
            _store.Verify(s => s.Provisionar(
                It.Is<SsoUserInfo>(i => i.Email == "nuevo@ucr.ac.cr"), 0), Times.Once);
        }

        [Fact]
        public void NewUser_PreRegisteredAsAsesor_IsPromotedToRoleTwo()
        {
            _store.Setup(s => s.ExisteUsuario("asesor@ucr.ac.cr")).Returns(false);
            _store.Setup(s => s.ExisteAsesor("asesor@ucr.ac.cr")).Returns(true);
            _store.Setup(s => s.Provisionar(It.IsAny<SsoUserInfo>(), 2));

            var result = CrearServicio().ResolveLogin(PrincipalConCorreo("asesor@ucr.ac.cr"));

            result.Success.Should().BeTrue();
            result.Rol.Should().Be(2);
            result.Provisioned.Should().BeTrue();
            _store.Verify(s => s.Provisionar(It.IsAny<SsoUserInfo>(), 2), Times.Once);
        }

        [Fact]
        public void NewUser_ProvisionInfo_IsBuiltFromClaims()
        {
            _store.Setup(s => s.ExisteUsuario("carlos@ucr.ac.cr")).Returns(false);
            _store.Setup(s => s.ExisteAsesor("carlos@ucr.ac.cr")).Returns(false);
            SsoUserInfo? capturado = null;
            _store.Setup(s => s.Provisionar(It.IsAny<SsoUserInfo>(), 0))
                  .Callback<SsoUserInfo, int>((info, _) => capturado = info);

            var principal = PrincipalConCorreo("carlos@ucr.ac.cr",
                ("given_name", "Carlos"),
                ("family_name", "Rojas"),
                ("ucrMotherSn", "Mora"),
                ("ucrUserId", "1-2345-6789"));

            CrearServicio().ResolveLogin(principal);

            capturado.Should().NotBeNull();
            capturado!.Nombre.Should().Be("Carlos");
            capturado.PrimerApellido.Should().Be("Rojas");
            capturado.SegundoApellido.Should().Be("Mora");
            capturado.NumeroIdentificacion.Should().Be("1-2345-6789");
        }

        [Fact]
        public void MissingEmailClaim_IsRejected_WithoutProvisioning()
        {
            var result = CrearServicio().ResolveLogin(PrincipalConCorreo(null));

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
            _store.Verify(s => s.Provisionar(It.IsAny<SsoUserInfo>(), It.IsAny<int>()), Times.Never);
            _store.Verify(s => s.ExisteUsuario(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void NonInstitutionalEmail_IsRejected()
        {
            var result = CrearServicio().ResolveLogin(PrincipalConCorreo("someone@gmail.com"));

            result.Success.Should().BeFalse();
            _store.Verify(s => s.ExisteUsuario(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void Email_IsNormalizedToLowercase()
        {
            _store.Setup(s => s.ExisteUsuario("mixed.case@ucr.ac.cr")).Returns(true);
            _store.Setup(s => s.ObtenerUsuario("mixed.case@ucr.ac.cr"))
                  .Returns(new LoginModel { id = "mixed.case@ucr.ac.cr", rol = 0, contrasena = "x" });

            var result = CrearServicio().ResolveLogin(PrincipalConCorreo("Mixed.Case@UCR.AC.CR"));

            result.Success.Should().BeTrue();
            result.Id.Should().Be("mixed.case@ucr.ac.cr");
        }

        [Fact]
        public void ExtractEmail_UsesConfiguredClaimOrder()
        {
            var options = new UcrSsoOptions { EmailClaimTypes = new List<string> { "mail" } };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim("mail", "from.mail.claim@ucr.ac.cr") }, "oidc"));

            CrearServicio(options).ExtractEmail(principal).Should().Be("from.mail.claim@ucr.ac.cr");
        }
    }
}

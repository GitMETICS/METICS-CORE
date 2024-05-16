// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using webMetics.Areas.Identity.Data;
using static System.Collections.Specialized.BitVector32;
using static System.Runtime.InteropServices.JavaScript.JSType;
using webMetics.Models;

namespace webMetics.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<webMeticsUser> _signInManager;
        private readonly UserManager<webMeticsUser> _userManager;
        private readonly IUserStore<webMeticsUser> _userStore;
        private readonly IUserEmailStore<webMeticsUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        public RegisterModel(
            UserManager<webMeticsUser> userManager,
            IUserStore<webMeticsUser> userStore,
            SignInManager<webMeticsUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            [Display(Name = "Número de identificación")]
            public string idParticipante { get; set; }

            [Required(ErrorMessage = "Es necesario ingresar un nombre.")]
            [Display(Name = "Nombre")]
            public string nombre { get; set; }

            [Required(ErrorMessage = "Es necesario ingresar un apellido.")]
            [Display(Name = "Primer apellido")]
            public string apellido_1 { get; set; }

            [Display(Name = "Segundo apellido")]
            public string apellido_2 { get; set; }

            [RegularExpression(@"[\w\.]+@ucr\.ac\.cr", ErrorMessage = "El correo electrónico debe terminar con '@ucr.ac.cr'.")]
            [Required(ErrorMessage = "Es necesario ingresar un correo institucional.")]
            [Display(Name = "Correo institucional")]
            public string correo { get; set; }

            [Required(ErrorMessage = "Es necesario ingresar un tipo de identificación.")]
            [Display(Name = "Tipo de identificación")]
            public string tipoIdentificacion { get; set; }

            [Required(ErrorMessage = "Es necesario ingresar un tipo de participante.")]
            [Display(Name = "Tipo de participante")]
            public string tipoParticipante { get; set; }

            [Required(ErrorMessage = "Es necesario ingresar una unidad académica.")]
            [Display(Name = "Unidad académica")]
            public string unidadAcademica { get; set; }

            [Required(ErrorMessage = "Es necesario ingresar un área.")]
            [Display(Name = "Área a la que pertenece")]
            public string area { get; set; }

            [Required(ErrorMessage = "Es necesario ingresar un departamento.")]
            [Display(Name = "Departamento al que pertenece")]
            public string departamento { get; set; }

            [Required(ErrorMessage = "Es necesario ingresar una sección.")]
            [Display(Name = "Sección a la que pertenece")]
            public string seccion { get; set; }

            [Required(ErrorMessage = "Es necesario ingresar una condición actual.")]
            [Display(Name = "Condición actual")]
            public string condicion { get; set; }

            [Required(ErrorMessage = "Es necesario ingresar un número de teléfono.")]
            [Display(Name = "Número de teléfono")]
            public string telefonos { get; set; }

            [Display(Name = "Horas matriculadas")]
            public int horasMatriculadas { get; set; }

            [Display(Name = "Horas aprobadas")]
            public int horasAprobadas { get; set; }


            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }


        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                var user = CreateUser();

                user.nombre = Input.nombre;
                user.apellido_1 = Input.apellido_1;
                user.apellido_2 = Input.apellido_2;
                user.correo = Input.Email;
                user.tipoIdentificacion = Input.tipoIdentificacion;
                user.unidadAcademica = Input.unidadAcademica;
                user.telefonos = Input.telefonos;
                user.condicion = Input.condicion;
                user.area = Input.area;
                user.departamento = Input.departamento;
                user.seccion = Input.seccion;

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        private webMeticsUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<webMeticsUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(webMeticsUser)}'. " +
                    $"Ensure that '{nameof(webMeticsUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<webMeticsUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<webMeticsUser>)_userStore;
        }

        public enum TipoIdentificacion
        {
            Cédula,
            Residente,
            Pasaporte
        }

        public enum TipoDeParticipantes
        {
            Profesor,
            Director,
            Asistente
        }
    }
}

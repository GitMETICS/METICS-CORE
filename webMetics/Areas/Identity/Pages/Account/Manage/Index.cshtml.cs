// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using webMetics.Areas.Identity.Data;

namespace webMetics.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<webMeticsUser> _userManager;
        private readonly SignInManager<webMeticsUser> _signInManager;

        public IndexModel(
            UserManager<webMeticsUser> userManager,
            SignInManager<webMeticsUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

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
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [DataType(DataType.Text)]
            [Display(Name = "Primer Nombre")]
            public string nombre { get; set; }

            [Required]
            [DataType(DataType.Text)]
            [Display(Name = "Primer Apellido")]
            public string apellido_1 { get; set; }

            [Required]
            [DataType(DataType.Text)]
            [Display(Name = "Segundo Apellido")]
            public string apellido_2 { get; set; }

            [Required]
            [DataType(DataType.Text)]
            [Display(Name = "Correo")]
            public string correo { get; set; }

            [Required]
            [DataType(DataType.Text)]
            [Display(Name = "Tipo Identificacion")]
            public string tipoIdentificacion { get; set; }

            [Required]
            [DataType(DataType.Text)]
            [Display(Name = "Número de identificación")]
            public string idParticipante { get; set; }

            [Required]
            [DataType(DataType.Text)]
            [Display(Name = "Unidad Académica")]
            public string unidadAcademica { get; set; }

            [Phone]
            [DataType(DataType.Text)]
            [Display(Name = "Número de telefono")]
            public string telefonos { get; set; }

            [DataType(DataType.Text)]
            public string condicion { get; set; }

            [Required]
            [DataType(DataType.Text)]
            [Display(Name = "Area")]
            public string area { get; set; }

            [Required]
            [DataType(DataType.Text)]
            [Display(Name = "Departamento")]
            public string departamento { get; set; }

            [Required]
            [DataType(DataType.Text)]
            [Display(Name = "Sección")]
            public string seccion { get; set; }

            [Required]
            [DataType(DataType.Text)]
            [Display(Name = "Horas matriculadas")]
            public string horasMatriculadas { get; set; }

            [Required]
            [DataType(DataType.Text)]
            [Display(Name = "Horas Aprobadas")]
            public string horasAprobadas { get; set; }
        }

        private async Task LoadAsync(webMeticsUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);

            Username = userName;

            Input = new InputModel
            {
                nombre = user.nombre,
                apellido_1 = user.apellido_1,
                apellido_2 = user.apellido_2,
                correo = user.correo,
                tipoIdentificacion = user.tipoIdentificacion,
                idParticipante = user.id,
                unidadAcademica = user.unidadAcademica,
                telefonos = user.telefonos,
                condicion = user.condicion,
                area = user.area,
                departamento = user.departamento,
                seccion = user.seccion,
                horasMatriculadas = user.horasMatriculadas,
                horasAprobadas = user.horasAprobadas
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var telefonos = await _userManager.GetPhoneNumberAsync(user);
            if (Input.telefonos != telefonos)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.telefonos);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            if (Input.nombre != user.nombre)
            {
                user.nombre = Input.nombre;
            }

            if (Input.apellido_1 != user.apellido_1)
            {
                user.apellido_1 = Input.apellido_1;
            }

            if (Input.apellido_2 != user.apellido_2)
            {
                user.apellido_2 = Input.apellido_2;
            }

            if (Input.correo != user.correo)
            {
                user.correo = Input.correo;
            }

            if (Input.tipoIdentificacion != user.tipoIdentificacion)
            {
                user.tipoIdentificacion = Input.tipoIdentificacion;
            }

            if (Input.idParticipante != user.id)
            {
                user.id = Input.idParticipante;
            }

            if (Input.unidadAcademica != user.unidadAcademica)
            {
                user.unidadAcademica = Input.unidadAcademica;
            }

            if (Input.telefonos != user.telefonos)
            {
                user.telefonos = Input.telefonos;
            }

            if (Input.condicion != user.condicion)
            {
                user.condicion = Input.condicion;
            }

            if (Input.area != user.area)
            {
                user.area = Input.area;
            }

            if (Input.area != user.area)
            {
                user.area = Input.area;
            }

            if (Input.departamento != user.departamento)
            {
                user.departamento = Input.departamento;
            }

            if (Input.seccion != user.seccion)
            {
                user.seccion = Input.seccion;
            }

            if (Input.horasMatriculadas != user.horasMatriculadas)
            {
                user.horasMatriculadas = Input.horasMatriculadas;
            }

            if (Input.horasAprobadas != user.horasAprobadas)
            {
                user.horasAprobadas = Input.horasAprobadas;
            }

            await _userManager.UpdateAsync(user);

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "El perfil ha sido actualizado";
            return RedirectToPage();
        }
    }
}

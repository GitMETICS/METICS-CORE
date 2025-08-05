using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace webMetics.Models
{
    public class UsuarioModel
    {
        public string? id { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un nombre.")]
        [Display(Name = "Nombre")]
        public required string nombre { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un apellido.")]
        [Display(Name = "Primer Apellido")]
        public required string primerApellido { get; set; }

        [Display(Name = "Segundo Apellido")]
        public string? segundoApellido { get; set; }

        [EmailDomain("ucr.ac.cr")]
        [Required(ErrorMessage = "Es necesario ingresar un correo institucional.")]
        [Display(Name = "Correo Institucional")]
        public string correo
        {
            get => _correo;
            set => _correo = EnsureEmailDomain(value);
        }
        private string _correo = string.Empty;


        [Required(ErrorMessage = "Es necesario ingresar un tipo de identificación.")]
        [Display(Name = "Tipo de Identificación")]
        public string? tipoIdentificacion { get; set; }

        [Display(Name = "Número de Identificación")]
        public string? numeroIdentificacion { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un área.")]
        [Display(Name = "Área")]
        public string? area { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar el Departamento o Facultad.")]
        [Display(Name = "Facultad o Departamento")]
        public string? departamento { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar una Unidad Académica.")]
        [Display(Name = "Unidad Académica")]
        public string? unidadAcademica { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un Recinto.")]
        [Display(Name = "Sede y Recinto")]
        public string? sede { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un tipo de participante.")]
        [Display(Name = "Tipo de Participante")]
        public string? tipoParticipante { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar una condición actual.")]
        [Display(Name = "Condición Actual en la Institución")]
        public string? condicion { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar un número de teléfono.")]
        [Display(Name = "Número de Teléfono")]
        public string? telefono { get; set; }

        public ParticipanteModel? participante { get; set; }


        private string EnsureEmailDomain(string email)
        {
            if (!string.IsNullOrWhiteSpace(email) && !email.Contains("@"))
            {
                email = $"{email}@ucr.ac.cr";
            }
            return email;
        }
    }

    public class LoginModel
    {

        [EmailDomain("ucr.ac.cr")]
        [Required(ErrorMessage = "Es necesario ingresar un correo institucional.")]
        [Display(Name = "Correo Institucional")]
        public string id
        {
            get => _id;
            set => _id = EnsureEmailDomain(value);
        }
        private string _id = string.Empty;

        [Display(Name = "Rol")]
        public int rol { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar una contraseña.")]
        [Display(Name = "Contraseña")]
        public string contrasena { get; set; }

        private string EnsureEmailDomain(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return email;
            }

            if (!email.Contains("@"))
            {
                email = $"{email}@ucr.ac.cr";
            }
            return email;
        }
    }

    public class NewLoginModel
    {
        [RegularExpression(@"[a-zA-Z0-9._%-]+@ucr\.ac\.cr", ErrorMessage = "El correo electrónico debe terminar con '@ucr.ac.cr'.")]
        [Required(ErrorMessage = "Es necesario ingresar un correo institucional.")]
        [Display(Name = "Correo institucional")]
        public string oldId { get; set; }

        [RegularExpression(@"[a-zA-Z0-9._%-]+@ucr\.ac\.cr", ErrorMessage = "El correo electrónico debe terminar con '@ucr.ac.cr'.")]
        [Required(ErrorMessage = "Es necesario ingresar un correo institucional.")]
        [Display(Name = "Correo institucional")]
        public string id { get; set; }

        public int role { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar su contraseña actual.")]
        [Display(Name = "Contraseña actual")]
        public string contrasena { get; set; }

        [Required(ErrorMessage = "Es necesario ingresar una contraseña.")]
        [Display(Name = "Nueva contraseña")]
        public string nuevaContrasena { get; set; }

        [Required(ErrorMessage = "Es necesario confirmar la nueva contraseña.")]
        [Display(Name = "Confirmar nueva contraseña")]
        public string confirmarContrasena { get; set; }

        [Display(Name = "Enviar contraseña por correo")]
        public bool enviarPorCorreo { get; set; }
    }
}

public class EmailDomainAttribute : ValidationAttribute
{
    private readonly string _domain;

    public EmailDomainAttribute(string domain)
    {
        _domain = domain;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var email = value as string;

        if (string.IsNullOrWhiteSpace(email))
        {
            return new ValidationResult("El correo electrónico es obligatorio.");
        }

        if (!email.Contains("@"))
        {
            email = $"{email}@{_domain}";
        }
        else if (!email.EndsWith(_domain))
        {
            return new ValidationResult($"El correo electrónico debe terminar con '{_domain}'.");
        }

        var regex = new Regex(@"^[a-zA-Z0-9._%-]+@ucr\.ac\.cr$");
        if (!regex.IsMatch(email))
        {
            return new ValidationResult($"El correo electrónico debe seguir el formato requerido y terminar con '{_domain}'.");
        }

        return ValidationResult.Success;
    }
}

public class BitacoraAcceso
{
    // id_acceso_PK (BIGINT IDENTITY)
    // La propiedad IdAccesoPK se mapea a la columna id_acceso_PK de la tabla.
    [Column("id_acceso_PK")]
    public long IdAccesoPK { get; set; }

    // id_usuario_FK (NVARCHAR(64))
    [Column("id_usuario_FK")]
    public string IdUsuarioFK { get; set; }

    // fecha_hora_acceso (DATETIME2(3))
    [Column("fecha_hora_acceso")]
    public DateTime FechaHoraAcceso { get; set; }

    // estado_acceso (NVARCHAR(20))
    [Column("estado_acceso")]
    public string EstadoAcceso { get; set; }
}
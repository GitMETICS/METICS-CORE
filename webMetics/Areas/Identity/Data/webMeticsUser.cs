using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace webMetics.Areas.Identity.Data;

// Add profile data for application users by adding properties to the webMeticsUser class
public class webMeticsUser : IdentityUser
{
    [PersonalData]
    public string? nombre { get; set; }
    [PersonalData]
    public string? apellido_1 { get; set; }
    [PersonalData]
    public string? apellido_2 { get; set; }
    [PersonalData]
    public string? correo { get; set; }
    [PersonalData]
    public string? tipoIdentificacion { get; set; }
    [PersonalData]
    public string? unidadAcademica { get; set; }
    [PersonalData]
    public string? telefonos { get; set; }
    [PersonalData]
    public string? condicion {  get; set; }
    [PersonalData]
    public string? area { get; set; }
    [PersonalData]
    public string? departamento { get; set; }
    [PersonalData]
    public string? seccion { get; set; }
    [PersonalData]
    public string? horasMatriculadas { get; set; }
    [PersonalData]
    public string? horasAprobadas { get; set; }
}


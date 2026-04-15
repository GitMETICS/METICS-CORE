using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using webMetics.Models;
using Xunit;

namespace webMetics.Tests.Models;

public class ParticipanteModelTests
{
    // Helper: builds a fully-valid model so individual properties can be overridden per test.
    private static ParticipanteModel ValidModel() => new()
    {
        nombre = "Ana",
        primerApellido = "García",
        correo = "ana.garcia@ucr.ac.cr",
        correoAlternativo = "ana.garcia@gmail.com",
        area = "Área de Artes",
        departamento = "Facultad de Artes",
        unidadAcademica = "Escuela de Artes",
        sede = "Ciudad Universitaria Rodrigo Facio",
        tipoParticipante = "Profesor",
        condicion = "Propiedad",
        telefono = "8888-8888",
        tipoIdentificacion = "Cédula",
        carrera = "Ninguna",
        horasMatriculadas = 0,
        horasAprobadas = 0,
    };

    private static IList<ValidationResult> Validate(ParticipanteModel model)
    {
        var context = new ValidationContext(model, null, null);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    // ── correo ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("user@ucr.ac.cr")]
    [InlineData("first.last@ucr.ac.cr")]
    [InlineData("a123_b.c@ucr.ac.cr")]
    public void Correo_ValidUcrAddress_PassesValidation(string correo)
    {
        var model = ValidModel();
        model.correo = correo;

        Validate(model).Should().BeEmpty();
    }

    [Theory]
    [InlineData("user@gmail.com")]
    [InlineData("user@ucr.co.cr")]
    [InlineData("notanemail")]
    [InlineData("@ucr.ac.cr")]
    public void Correo_NonUcrAddress_FailsValidation(string correo)
    {
        var model = ValidModel();
        model.correo = correo;

        Validate(model).Should().Contain(r => r.MemberNames.Contains(nameof(ParticipanteModel.correo)));
    }

    // ── correoAlternativo ─────────────────────────────────────────────────────

    [Fact]
    public void CorreoAlternativo_InvalidFormat_FailsValidation()
    {
        var model = ValidModel();
        model.correoAlternativo = "not-an-email";

        Validate(model).Should().Contain(r => r.MemberNames.Contains(nameof(ParticipanteModel.correoAlternativo)));
    }

    [Fact]
    public void CorreoAlternativo_ValidEmail_PassesValidation()
    {
        var model = ValidModel();
        model.correoAlternativo = "valid@example.com";

        Validate(model).Should().BeEmpty();
    }

    // ── numeroIdentificacion ──────────────────────────────────────────────────

    [Theory]
    [InlineData("1-2345-6789")]   // cédula with dashes
    [InlineData("123456789")]     // 9 digits, no dashes
    public void NumeroIdentificacion_ValidFormats_PassesValidation(string id)
    {
        var model = ValidModel();
        model.numeroIdentificacion = id;

        Validate(model).Should().BeEmpty();
    }

    [Theory]
    [InlineData("12-345-678")]    // wrong dash placement
    [InlineData("1234567890")]    // 10 digits
    [InlineData("ABC-1234-5678")] // letters
    [InlineData("12345678")]      // 8 digits
    public void NumeroIdentificacion_InvalidFormats_FailsValidation(string id)
    {
        var model = ValidModel();
        model.numeroIdentificacion = id;

        Validate(model).Should().Contain(r => r.MemberNames.Contains(nameof(ParticipanteModel.numeroIdentificacion)));
    }

    // ── hours range ───────────────────────────────────────────────────────────

    [Fact]
    public void HorasMatriculadas_Negative_FailsValidation()
    {
        var model = ValidModel();
        model.horasMatriculadas = -1;

        Validate(model).Should().Contain(r => r.MemberNames.Contains(nameof(ParticipanteModel.horasMatriculadas)));
    }

    [Fact]
    public void HorasAprobadas_Negative_FailsValidation()
    {
        var model = ValidModel();
        model.horasAprobadas = -5;

        Validate(model).Should().Contain(r => r.MemberNames.Contains(nameof(ParticipanteModel.horasAprobadas)));
    }

    [Fact]
    public void HorasMatriculadas_Zero_PassesValidation()
    {
        var model = ValidModel();
        model.horasMatriculadas = 0;

        Validate(model).Should().BeEmpty();
    }

    // ── required fields ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(nameof(ParticipanteModel.nombre))]
    [InlineData(nameof(ParticipanteModel.primerApellido))]
    [InlineData(nameof(ParticipanteModel.correo))]
    [InlineData(nameof(ParticipanteModel.area))]
    [InlineData(nameof(ParticipanteModel.departamento))]
    [InlineData(nameof(ParticipanteModel.unidadAcademica))]
    [InlineData(nameof(ParticipanteModel.sede))]
    [InlineData(nameof(ParticipanteModel.tipoParticipante))]
    [InlineData(nameof(ParticipanteModel.condicion))]
    [InlineData(nameof(ParticipanteModel.telefono))]
    [InlineData(nameof(ParticipanteModel.carrera))]
    public void RequiredField_WhenNull_FailsValidation(string propertyName)
    {
        var model = ValidModel();
        typeof(ParticipanteModel).GetProperty(propertyName)!.SetValue(model, null);

        Validate(model).Should().Contain(r => r.MemberNames.Contains(propertyName));
    }
}

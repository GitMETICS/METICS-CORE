using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using webMetics.Controllers;
using webMetics.Handlers;
using webMetics.Models;
using Xunit;

namespace webMetics.Tests.Controllers;

/// <summary>
/// Tests the Areas Extra validation wired into FormularioParticipante POST.
/// Uses the internal test constructor so no real database connection is needed.
/// </summary>
public class ParticipanteControllerAreasExtraTests
{
    private static readonly List<string> FakeCatalog = ["Área de Artes", "Área de Ciencias", "Área de Ingeniería"];

    // Builds a controller wired to a mock handler + a default HTTP context.
    private static ParticipanteController BuildController(Mock<IParticipanteHandler> mockHandler)
    {
        var controller = new ParticipanteController(mockHandler.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    // Minimal valid-looking model (DataAnnotation model binding doesn't run in unit tests,
    // so ModelState starts clean; required fields are set to keep the C# required modifier happy).
    private static ParticipanteModel MinimalModel(List<string>? areasExtra = null) => new()
    {
        nombre = "Ana",
        primerApellido = "García",
        correo = "ana@ucr.ac.cr",
        area = "Área de Artes",
        departamento = "Facultad de Artes",
        unidadAcademica = "Escuela de Artes",
        sede = "Sede Rodrigo Facio",
        tipoParticipante = "Profesor",
        condicion = "Propiedad",
        telefono = "8888-8888",
        tipoIdentificacion = "Cédula",
        carrera = "Ninguna",
        areasExtra = areasExtra ?? [],
    };

    // ── invalid extra area adds ModelState error ──────────────────────────────

    [Fact]
    public void FormularioParticipante_Post_InvalidExtraArea_AddsModelError()
    {
        var mockHandler = new Mock<IParticipanteHandler>();
        mockHandler.Setup(h => h.GetAllAreas()).Returns(FakeCatalog);
        var controller = BuildController(mockHandler);
        // Force the else-path so we never reach IngresarParticipante (which needs a real DB).
        controller.ModelState.AddModelError("_dummy", "force else path");

        var model = MinimalModel(["Area Que No Existe"]);
        controller.FormularioParticipante(model);

        controller.ModelState.Keys.Should().Contain(nameof(ParticipanteModel.areasExtra));
    }

    // ── valid extra areas produce no areasExtra error ─────────────────────────

    [Fact]
    public void FormularioParticipante_Post_ValidExtraArea_NoAreasExtraError()
    {
        var mockHandler = new Mock<IParticipanteHandler>();
        mockHandler.Setup(h => h.GetAllAreas()).Returns(FakeCatalog);
        var controller = BuildController(mockHandler);
        controller.ModelState.AddModelError("_dummy", "force else path");

        var model = MinimalModel(["Área de Ciencias"]);
        controller.FormularioParticipante(model);

        controller.ModelState.Keys.Should().NotContain(nameof(ParticipanteModel.areasExtra));
    }

    // ── empty areasExtra skips validation entirely ────────────────────────────

    [Fact]
    public void FormularioParticipante_Post_EmptyAreasExtra_NoAreasExtraError()
    {
        var mockHandler = new Mock<IParticipanteHandler>();
        // GetAllAreas should NOT be called when areasExtra is empty.
        mockHandler.Setup(h => h.GetAllAreas()).Returns(FakeCatalog);
        var controller = BuildController(mockHandler);
        controller.ModelState.AddModelError("_dummy", "force else path");

        var model = MinimalModel([]);
        controller.FormularioParticipante(model);

        controller.ModelState.Keys.Should().NotContain(nameof(ParticipanteModel.areasExtra));
        // GetAllAreas is called once for ViewData["jsonDataAreas"] in the else branch,
        // but NOT for areasExtra validation (ValidarAreasExtra returns early for empty lists).
        mockHandler.Verify(h => h.GetAllAreas(), Times.Once);
    }

    // ── mixed valid + invalid produces error ──────────────────────────────────

    [Fact]
    public void FormularioParticipante_Post_MixedAreas_AddsModelError()
    {
        var mockHandler = new Mock<IParticipanteHandler>();
        mockHandler.Setup(h => h.GetAllAreas()).Returns(FakeCatalog);
        var controller = BuildController(mockHandler);
        controller.ModelState.AddModelError("_dummy", "force else path");

        var model = MinimalModel(["Área de Ciencias", "Área Inventada"]);
        controller.FormularioParticipante(model);

        controller.ModelState.Keys.Should().Contain(nameof(ParticipanteModel.areasExtra));
    }

    // ── result is a ViewResult when ModelState is invalid ────────────────────

    [Fact]
    public void FormularioParticipante_Post_InvalidModel_ReturnsView()
    {
        var mockHandler = new Mock<IParticipanteHandler>();
        mockHandler.Setup(h => h.GetAllAreas()).Returns(FakeCatalog);
        var controller = BuildController(mockHandler);
        controller.ModelState.AddModelError("_dummy", "force else path");

        var result = controller.FormularioParticipante(MinimalModel());

        result.Should().BeOfType<ViewResult>()
            .Which.ViewName.Should().Be("FormularioParticipante");
    }
}

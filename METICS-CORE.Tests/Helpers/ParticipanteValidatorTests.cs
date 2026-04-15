using FluentAssertions;
using webMetics.Helpers;
using Xunit;

namespace webMetics.Tests.Helpers;

public class ParticipanteValidatorTests
{
    private static readonly List<string> Catalog = ["Área de Artes", "Área de Ciencias", "Área de Ingeniería"];

    // ── FiltrarAreasExtraValidas ───────────────────────────────────────────────

    [Fact]
    public void Filtrar_NullInput_ReturnsEmptyList()
    {
        ParticipanteValidator.FiltrarAreasExtraValidas(null, "Área de Artes", Catalog)
            .Should().BeEmpty();
    }

    [Fact]
    public void Filtrar_EmptyInput_ReturnsEmptyList()
    {
        ParticipanteValidator.FiltrarAreasExtraValidas([], "Área de Artes", Catalog)
            .Should().BeEmpty();
    }

    [Fact]
    public void Filtrar_PrimaryAreaExcluded_ExactMatch()
    {
        var selected = new List<string> { "Área de Artes", "Área de Ciencias" };

        var result = ParticipanteValidator.FiltrarAreasExtraValidas(selected, "Área de Artes", Catalog);

        result.Should().ContainSingle().Which.Should().Be("Área de Ciencias");
    }

    [Fact]
    public void Filtrar_PrimaryAreaExcluded_CaseInsensitive()
    {
        var selected = new List<string> { "área de artes", "Área de Ciencias" };

        var result = ParticipanteValidator.FiltrarAreasExtraValidas(selected, "Área de Artes", Catalog);

        result.Should().ContainSingle().Which.Should().Be("Área de Ciencias");
    }

    [Fact]
    public void Filtrar_DuplicatesRemoved_CaseInsensitive()
    {
        var selected = new List<string> { "Área de Ciencias", "área de ciencias", "ÁREA DE CIENCIAS" };

        var result = ParticipanteValidator.FiltrarAreasExtraValidas(selected, null, Catalog);

        result.Should().ContainSingle();
    }

    [Fact]
    public void Filtrar_InvalidAreasExcluded()
    {
        var selected = new List<string> { "Área Inventada", "Área de Ciencias" };

        var result = ParticipanteValidator.FiltrarAreasExtraValidas(selected, null, Catalog);

        result.Should().ContainSingle().Which.Should().Be("Área de Ciencias");
    }

    [Fact]
    public void Filtrar_AllInvalid_ReturnsEmptyList()
    {
        var selected = new List<string> { "No existe", "Tampoco" };

        ParticipanteValidator.FiltrarAreasExtraValidas(selected, null, Catalog)
            .Should().BeEmpty();
    }

    [Fact]
    public void Filtrar_WhitespaceEntries_AreExcluded()
    {
        var selected = new List<string> { "  ", "", "Área de Ingeniería" };

        var result = ParticipanteValidator.FiltrarAreasExtraValidas(selected, null, Catalog);

        result.Should().ContainSingle().Which.Should().Be("Área de Ingeniería");
    }

    [Fact]
    public void Filtrar_MixedValidInvalidPrimaryDuplicate_ReturnsCleanList()
    {
        // primary: Área de Artes
        // selected: valid-dup, valid-dup (case), primary, invalid, valid-unique
        var selected = new List<string>
        {
            "Área de Ciencias",
            "área de ciencias",   // duplicate of above
            "Área de Artes",      // same as primary → excluded
            "Inventada",          // not in catalog → excluded
            "Área de Ingeniería", // valid, unique
        };

        var result = ParticipanteValidator.FiltrarAreasExtraValidas(selected, "Área de Artes", Catalog);

        result.Should().BeEquivalentTo(["Área de Ciencias", "Área de Ingeniería"]);
    }

    // ── AreasExtraSonValidas ──────────────────────────────────────────────────

    [Fact]
    public void AreasExtraSonValidas_AllInCatalog_ReturnsTrue()
    {
        var areas = new List<string> { "Área de Artes", "Área de Ciencias" };

        ParticipanteValidator.AreasExtraSonValidas(areas, Catalog).Should().BeTrue();
    }

    [Fact]
    public void AreasExtraSonValidas_OneNotInCatalog_ReturnsFalse()
    {
        var areas = new List<string> { "Área de Artes", "Área Inventada" };

        ParticipanteValidator.AreasExtraSonValidas(areas, Catalog).Should().BeFalse();
    }

    [Fact]
    public void AreasExtraSonValidas_CaseInsensitiveMatch_ReturnsTrue()
    {
        var areas = new List<string> { "área de artes", "ÁREA DE CIENCIAS" };

        ParticipanteValidator.AreasExtraSonValidas(areas, Catalog).Should().BeTrue();
    }

    [Fact]
    public void AreasExtraSonValidas_EmptyAreas_ReturnsTrue()
    {
        ParticipanteValidator.AreasExtraSonValidas([], Catalog).Should().BeTrue();
    }
}

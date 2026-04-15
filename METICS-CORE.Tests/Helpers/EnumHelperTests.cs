using FluentAssertions;
using webMetics.Helpers;
using webMetics.Models;
using Xunit;

namespace webMetics.Tests.Helpers;

public class EnumHelperTests
{
    // ── GetDisplayName(Enum) ──────────────────────────────────────────────────

    [Theory]
    [InlineData(TipoDeParticipantes.Profesor, "Profesor(a)")]
    [InlineData(TipoDeParticipantes.Director, "Director(a)")]
    [InlineData(TipoDeParticipantes.Asistente, "Asistente")]
    public void GetDisplayName_TipoDeParticipantes_ReturnsDisplayName(TipoDeParticipantes value, string expected)
    {
        EnumHelper.GetDisplayName(value).Should().Be(expected);
    }

    [Theory]
    [InlineData(TipoModalidad.Autogestionado, "Autogestionado")]
    [InlineData(TipoModalidad.Presencial, "Presencial")]
    [InlineData(TipoModalidad.BajoVirtual, "Bajo Virtual")]
    [InlineData(TipoModalidad.Bimodal, "Bimodal")]
    [InlineData(TipoModalidad.AltoVirtual, "Alto Virtual")]
    [InlineData(TipoModalidad.Virtual, "Virtual")]
    public void GetDisplayName_TipoModalidad_ReturnsDisplayName(TipoModalidad value, string expected)
    {
        EnumHelper.GetDisplayName(value).Should().Be(expected);
    }

    // ── GetModalidadFromDisplayName ───────────────────────────────────────────

    [Theory]
    [InlineData("Autogestionado", TipoModalidad.Autogestionado)]
    [InlineData("Presencial", TipoModalidad.Presencial)]
    [InlineData("Bajo Virtual", TipoModalidad.BajoVirtual)]
    [InlineData("Bimodal", TipoModalidad.Bimodal)]
    [InlineData("Alto Virtual", TipoModalidad.AltoVirtual)]
    [InlineData("Virtual", TipoModalidad.Virtual)]
    public void GetModalidadFromDisplayName_ValidName_ReturnsCorrectEnum(string displayName, TipoModalidad expected)
    {
        EnumHelper.GetModalidadFromDisplayName(displayName).Should().Be(expected);
    }

    [Fact]
    public void GetModalidadFromDisplayName_InvalidName_ThrowsArgumentException()
    {
        var act = () => EnumHelper.GetModalidadFromDisplayName("NoExiste");

        act.Should().Throw<ArgumentException>();
    }

    // ── Round-trip: GetDisplayName ↔ GetModalidadFromDisplayName ─────────────

    [Theory]
    [InlineData(TipoModalidad.BajoVirtual)]
    [InlineData(TipoModalidad.AltoVirtual)]
    [InlineData(TipoModalidad.Virtual)]
    public void GetDisplayName_ThenGetModalidadFromDisplayName_RoundTrips(TipoModalidad value)
    {
        var displayName = EnumHelper.GetDisplayName(value);
        var roundTripped = EnumHelper.GetModalidadFromDisplayName(displayName);

        roundTripped.Should().Be(value);
    }
}

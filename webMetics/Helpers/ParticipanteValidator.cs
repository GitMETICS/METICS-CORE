namespace webMetics.Helpers;

public static class ParticipanteValidator
{
    /// <summary>
    /// Returns true when every area in <paramref name="areasExtra"/> exists in
    /// <paramref name="validAreas"/> (case-insensitive).
    /// </summary>
    public static bool AreasExtraSonValidas(IEnumerable<string> areasExtra, IEnumerable<string> validAreas)
    {
        var catalog = new HashSet<string>(validAreas, StringComparer.OrdinalIgnoreCase);
        return areasExtra.All(area => catalog.Contains(area));
    }

    /// <summary>
    /// Filters <paramref name="selected"/> areas: keeps only entries that are
    /// non-empty, present in <paramref name="validAreas"/>, not equal to
    /// <paramref name="primary"/> (case-insensitive), and deduplicated.
    /// </summary>
    public static List<string> FiltrarAreasExtraValidas(
        List<string>? selected,
        string? primary,
        IEnumerable<string> validAreas)
    {
        if (selected == null || selected.Count == 0)
            return new List<string>();

        var catalog = new HashSet<string>(validAreas, StringComparer.OrdinalIgnoreCase);

        return selected
            .Where(area => !string.IsNullOrWhiteSpace(area))
            .Where(area => catalog.Contains(area))
            .Where(area => !string.Equals(area, primary, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

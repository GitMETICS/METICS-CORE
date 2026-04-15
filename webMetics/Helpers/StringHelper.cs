using System.Globalization;
using System.Text;

namespace webMetics.Helpers;

public static class StringHelper
{
    /// <summary>
    /// Removes diacritical marks (accents) from a string, e.g. "José" → "Jose".
    /// </summary>
    public static string RemoveAccents(string text)
    {
        return string.Concat(text.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
            .Normalize(NormalizationForm.FormC);
    }
}

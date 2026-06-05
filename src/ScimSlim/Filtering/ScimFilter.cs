using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace ScimSlim.Filtering;

/// <summary>
/// A parsed SCIM filter expression. ScimSlim only supports the simple
/// <c>attribute op "value"</c> form that Authentik emits (e.g.
/// <c>userName eq "jdoe"</c>, <c>externalId eq "abc123"</c>).
/// </summary>
/// <param name="Attribute">The attribute name, e.g. <c>userName</c>.</param>
/// <param name="Operator">The comparison operator, lower-cased, e.g. <c>eq</c>.</param>
/// <param name="Value">The unquoted comparison value.</param>
public readonly record struct ScimFilter(string Attribute, string Operator, string Value)
{
    // attribute  operator  "value"   — value may contain escaped quotes.
    private static readonly Regex Pattern = new(
        """^\s*(?<attr>[\w.:]+)\s+(?<op>\w+)\s+"(?<val>(?:\\.|[^"\\])*)"\s*$""",
        RegexOptions.Compiled);

    /// <summary>
    /// Attempts to parse a single-comparison SCIM filter. Returns false for null,
    /// empty, or unsupported (compound) expressions.
    /// </summary>
    public static bool TryParse(string? filter, [NotNullWhen(true)] out ScimFilter? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(filter))
        {
            return false;
        }

        var match = Pattern.Match(filter);
        if (!match.Success)
        {
            return false;
        }

        var value = match.Groups["val"].Value
            .Replace("\\\"", "\"")
            .Replace("\\\\", "\\");

        result = new ScimFilter(
            match.Groups["attr"].Value,
            match.Groups["op"].Value.ToLowerInvariant(),
            value);
        return true;
    }
}

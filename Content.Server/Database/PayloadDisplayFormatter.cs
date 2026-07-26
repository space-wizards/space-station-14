using System.Text.Json;

namespace Content.Server.Database;

/// <summary>
/// Server-side helper that converts a payload JSON string into a human-readable display lines for the admin-log and audit-log UIs.
/// </summary>
/// <remarks>
/// Parsing happens server-side because Engine sandbox forbids
/// <c>System.Text.Json</c> parsing types (<c>JsonDocument</c>, <c>JsonElement</c>, etc.).
/// The resulting <c>string[]</c> contains only primitives, safe for the client.
/// </remarks>
public static class PayloadDisplayFormatter
{
    private const int MaxLines = 40;
    private const int MaxValueLength = 200;

    public static string[]? FormatPayloadLines(string? payloadJson)
    {
        if (string.IsNullOrEmpty(payloadJson))
            return null;

        var trimmed = payloadJson.Trim();
        if (trimmed == "{}" || trimmed == "{ }")
            return null;

        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return new[] { TruncateValue(trimmed) };

            var lines = new List<string>(16);
            // Write schemaVersion last so it doesn't clutter.
            JsonProperty? schemaVersionProp = null;

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (lines.Count >= MaxLines - 1)
                {
                    lines.Add("...(truncated)");
                    break;
                }

                if (prop.Name == "schemaVersion")
                {
                    schemaVersionProp = prop;
                    continue;
                }

                FlattenProperty(prop.Name, prop.Value, lines);
            }

            if (schemaVersionProp.HasValue)
                lines.Add($"[schema v{schemaVersionProp.Value.Value.GetRawText().Trim('"')}]");

            return lines.Count == 0 ? null : lines.ToArray();
        }
        catch
        {
            return new[] { TruncateValue(trimmed) };
        }
    }

    private static void FlattenProperty(
        string keyPath,
        JsonElement value,
        List<string> lines)
    {
        if (lines.Count >= MaxLines)
            return;

        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var child in value.EnumerateObject())
                    FlattenProperty($"{keyPath}.{child.Name}", child.Value, lines);
                break;

            case JsonValueKind.Array:
                var idx = 0;
                foreach (var element in value.EnumerateArray())
                {
                    if (lines.Count >= MaxLines)
                        break;
                    if (element.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var child in element.EnumerateObject())
                            FlattenProperty($"{keyPath}[{idx}].{child.Name}", child.Value, lines);
                    }
                    else
                    {
                        lines.Add($"{keyPath}[{idx}]: {TruncateValue(GetScalarDisplay(element))}");
                    }
                    idx++;
                }
                break;

            default:
                lines.Add($"{keyPath}: {TruncateValue(GetScalarDisplay(value))}");
                break;
        }
    }

    private static string GetScalarDisplay(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.True   => "true",
            JsonValueKind.False  => "false",
            JsonValueKind.Null   => "null",
            _                    => element.GetRawText()
        };
    }

    private static string TruncateValue(string value)
    {
        if (value.Length <= MaxValueLength)
            return value;
        return value[..MaxValueLength] + "\u2026"; // ellipsis
    }
}

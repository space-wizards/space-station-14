using System.Text.Json;
using System.Text.Json.Serialization;

namespace Content.Server.Administration.Logs;

public static class AdminLogJsonOptions
{
    public static readonly JsonSerializerOptions Minimal = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}

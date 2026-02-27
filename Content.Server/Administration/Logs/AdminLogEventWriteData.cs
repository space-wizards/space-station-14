using System.Text.Json;
using Content.Server.Database;
using Content.Shared.Database;

namespace Content.Server.Administration.Logs;

public sealed class AdminLogEventWriteData
{
    public required int RoundId { get; init; }
    public required LogType Type { get; init; }
    public required LogImpact Impact { get; init; }
    public required DateTime OccurredAt { get; init; }
    public required string Message { get; init; }
    public required JsonDocument Json { get; init; }
    public required List<Guid> Players { get; init; }
    public required List<AdminLogEventEntityWriteData> Entities { get; init; }
}

public sealed class AdminLogEventEntityWriteData
{
    public required int EntityUid { get; init; }
    public required AdminLogEntityRole Role { get; init; }
    public string? PrototypeId { get; init; }
    public string? EntityName { get; init; }
}

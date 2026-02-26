using System.Threading;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.Database;

namespace Content.Server.Administration.Logs;

//This whole thing is waiting for a faster third party indexing solution

/// <summary>
/// Structured admin log payload NOT IMPLEMENTED, but could be used by external indexers ... eventually ...
/// </summary>
/// <param name="RoundId"></param>
/// <param name="LogId"></param>
/// <param name="Type"></param>
/// <param name="Impact"></param>
/// <param name="Date"></param>
/// <param name="Message"></param>
/// <param name="Json"></param>
/// <param name="Players"></param>
/// <param name="Entities"></param>
public sealed record StructuredAdminLogEvent(
    int RoundId,
    int LogId,
    LogType Type,
    LogImpact Impact,
    DateTime Date,
    string Message,
    JsonDocument Json,
    IReadOnlyList<Guid> Players,
    IReadOnlyList<AdminLogEntityPayload> Entities);

public sealed record AdminLogEntityPayload(
    int EntityUid,
    AdminLogEntityRole Role,
    string? PrototypeId,
    string? EntityName);

/// <summary>
///  Extension point for forwarding structured admin logs to third-party indexing backends
/// </summary>
public interface IAdminLogEventPublisher
{
    ValueTask PublishAsync(StructuredAdminLogEvent logEvent, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}

public sealed class NullAdminLogEventPublisher : IAdminLogEventPublisher
{
    public ValueTask PublishAsync(StructuredAdminLogEvent logEvent, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}

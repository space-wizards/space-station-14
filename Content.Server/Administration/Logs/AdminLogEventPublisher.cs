using System.Threading;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Shared.Database;

namespace Content.Server.Administration.Logs;

//This whole thing is waiting for a faster third party indexing solution ... eventually ...

/// <summary>
/// Structured admin log payload NOT IMPLEMENTED
/// </summary>
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
    ValueTask PublishAsync(StructuredAdminLogEvent logEvent, CancellationToken cancellationToken = default);
}

public sealed class NullAdminLogEventPublisher : IAdminLogEventPublisher
{
    public ValueTask PublishAsync(StructuredAdminLogEvent logEvent, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}

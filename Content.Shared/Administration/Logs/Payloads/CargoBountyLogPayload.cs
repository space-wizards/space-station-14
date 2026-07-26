namespace Content.Shared.Administration.Logs.Payloads;

/// <summary>
/// Payload for cargo bounty events.
/// Used with <c>LogType.CargoBounty</c>.
/// </summary>
/// <param name="BountyInstanceId"></param>
/// <param name="BountyPrototype"></param>
/// <param name="Status">Lifecycle status: <c>"Added"</c>, <c>"Fulfilled"</c>, or <c>"Removed"</c>.</param>
public sealed record CargoBountyLogPayload(
    string BountyInstanceId,
    string BountyPrototype,
    string Status) : IVersionedPayload
{
    /// <inheritdoc/>
    public int SchemaVersion { get; } = 1;
}

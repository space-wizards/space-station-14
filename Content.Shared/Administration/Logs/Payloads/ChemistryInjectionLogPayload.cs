namespace Content.Shared.Administration.Logs.Payloads;

/// <summary>
/// Payload for chemical injection and ingestion events.
/// Covers <c>LogType.Ingestion</c> (injections, ingestion, forced feeding) and
/// <c>LogType.ChemicalReaction</c>.
/// </summary>
/// <remarks>
/// Actor, victim, and injector are captured as participants.
/// </remarks>
/// <param name="Reagents">List of reagents transferred with their quantities.</param>
/// <param name="TotalVolume">Total volume transferred as <c>FixedPoint2.Int()</c>.</param>
/// <param name="TransferDirection">
/// Direction of transfer: <c>"Inject"</c>, <c>"Draw"</c>
/// , or <c>"Ingest"</c>.
/// </param>
public sealed record ChemistryInjectionLogPayload(
    IReadOnlyList<ReagentSnapshot> Reagents,
    int TotalVolume,
    string TransferDirection) : IVersionedPayload
{
    /// <inheritdoc/>
    public int SchemaVersion { get; } = 1;
}

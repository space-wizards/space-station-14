namespace Content.Shared.Administration.Logs.Payloads;

/// <summary>
/// Payload for silicon law change events (<c>LogType.SiliconLaw</c>).
/// </summary>
/// <param name="PreviousLaws">
/// Ordered list of <c>SiliconLaw.LawString</c> values before the change.
/// <em>Note:</em> prototype-based laws store their locale key (e.g. <c>"law-borg-1"</c>);
/// emag and ion-storm laws store the already-resolved text.
/// </param>
/// <param name="NewLaws">
/// Ordered list of <c>SiliconLaw.LawString</c> values after the change.
/// </param>
/// <param name="ChangeType">
/// Type of change: <c>"Full"</c>, <c>"Add"</c>, <c>"Remove"</c>,
/// <c>"Reorder"</c>, or <c>"IonStorm"</c>.
/// </param>
/// <param name="ChangedLawIndex">
/// Index of the specific law that changed, or null for full replacements
/// and cases where the index is not meaningful.
/// </param>
public sealed record SiliconLawChangeLogPayload(
    IReadOnlyList<string> PreviousLaws,
    IReadOnlyList<string> NewLaws,
    string ChangeType,
    int? ChangedLawIndex = null) : IVersionedPayload
{
    /// <inheritdoc/>
    public int SchemaVersion { get; } = 1;
}

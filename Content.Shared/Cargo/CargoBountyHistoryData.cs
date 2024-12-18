using Robust.Shared.Serialization;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Cargo;

/// <summary>
/// A data structure for storing historical information about bounties.
/// </summary>
[DataDefinition, NetSerializable, Serializable]
public readonly partial record struct CargoBountyHistoryData
{
    /// <summary>
    /// A unique id used to identify the bounty
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Optional name of the actor that skipped the bounty.
    /// Only set when the bounty has been skipped.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string? ActorName { get; init; } = default;

    /// <summary>
    /// Time when this bounty was completed or skipped
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Timestamp { get; init; } = TimeSpan.MinValue;

    /// <summary>
    /// The prototype containing information about the bounty.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(required: true)]
    public ProtoId<CargoBountyPrototype> Bounty { get; init; } = string.Empty;

    public CargoBountyHistoryData(CargoBountyData bounty, TimeSpan timestamp, string? actorName)
    {
        Bounty = bounty.Bounty;
        Id = bounty.Id;
        ActorName = actorName;
        Timestamp = timestamp;
    }
}

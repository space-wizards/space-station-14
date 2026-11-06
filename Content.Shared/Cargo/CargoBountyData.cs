using Robust.Shared.Serialization;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Cargo;

/// <summary>
/// A data structure for storing currently available bounties.
/// </summary>
[DataDefinition, NetSerializable, Serializable]
public readonly partial record struct CargoBountyData
{
    /// <summary>
    /// A unique id used to identify the bounty
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// The prototype containing information about the bounty.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(required: true)]
    public ProtoId<CargoBountyPrototype> Bounty { get; init; } = string.Empty;

    /// <summary>
    /// Character names of players who printed labels for this bounty.
    /// </summary>
    [DataField]
    public List<string> ClaimedBy { get; init; } = new();

    /// <summary>
    /// Current status of the bounty on the bounty computer.
    /// </summary>
    [DataField]
    public string Status { get; init; } = string.Empty;

    public CargoBountyData(CargoBountyPrototype bounty, string bountyStatus, int uniqueIdentifier)
    {
        Bounty = bounty.ID;
        Id = $"{bounty.IdPrefix}{uniqueIdentifier:D3}";
        Status = bountyStatus;
    }
}

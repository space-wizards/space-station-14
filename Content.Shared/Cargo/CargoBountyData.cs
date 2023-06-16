using Robust.Shared.Serialization;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Cargo;

/// <summary>
/// A data structure for storing currently available bounties.
/// </summary>
[DataDefinition, NetSerializable, Serializable]
public readonly record struct CargoBountyData(int Id, CargoBountyPrototype Bounty, TimeSpan EndTime)
{
    /// <summary>
    /// A numeric id used to identify the bounty
    /// </summary>
    [DataField("id"), ViewVariables(VVAccess.ReadWrite)]
    public readonly int Id = Id;

    /// <summary>
    /// The prototype containing information about the bounty.
    /// </summary>
    [DataField("bounty"), ViewVariables(VVAccess.ReadWrite)]
    public readonly CargoBountyPrototype Bounty = Bounty;

    /// <summary>
    /// The time at which the bounty is closed and no longer is available.
    /// </summary>
    [DataField("endTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public readonly TimeSpan EndTime = EndTime;
}

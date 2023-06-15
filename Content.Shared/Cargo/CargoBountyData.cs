using Robust.Shared.Serialization;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Cargo;

[DataDefinition, NetSerializable, Serializable]
public readonly record struct CargoBountyData(int BountyId, CargoBountyPrototype Bounty, TimeSpan EndTime)
{
    [DataField("bountyId"), ViewVariables(VVAccess.ReadWrite)]
    public readonly int BountyId = BountyId;

    [DataField("bounty"), ViewVariables(VVAccess.ReadWrite)]
    public readonly CargoBountyPrototype Bounty = Bounty;

    [DataField("endTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public readonly TimeSpan EndTime = EndTime;
}

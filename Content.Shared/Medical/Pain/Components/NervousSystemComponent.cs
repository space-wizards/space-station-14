using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Pain.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class NervousSystemComponent : Component
{

    [DataField, AutoNetworkedField]
    public FixedPoint2 RawPain = FixedPoint2.Zero;

    [DataField, AutoNetworkedField]
    public FixedPoint2 Multiplier = 1f;

    [DataField, AutoNetworkedField]
    public SortedDictionary<FixedPoint2, MedicalConditionThreshold> ConditionThresholds= new();

    [DataField, AutoNetworkedField]
    public FixedPoint2 NominalMaxPain = 100;

    [DataField, AutoNetworkedField]
    public FixedPoint2 MitigatedPercentage = 0;

    public FixedPoint2 Pain => RawPain * Multiplier - RawPain * MitigatedPercentage/100;

    [NetSerializable, Serializable]
    [DataRecord]
    public record struct MedicalConditionThreshold(EntProtoId ConditionId, bool Applied);
}

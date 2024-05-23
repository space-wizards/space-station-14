using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Pain.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NervesComponent : Component
{

    [DataField, AutoNetworkedField]
    public FixedPoint2 RawPain = FixedPoint2.Zero;

    [DataField, AutoNetworkedField]
    public FixedPoint2 Multiplier = 1f;

    [DataField, AutoNetworkedField]
    public Dictionary<FixedPoint2, MedicalConditionThreshold> ConditionThresholds = new();

    [DataField, AutoNetworkedField]
    public FixedPoint2 RawPainCap = 100;

    [DataField, AutoNetworkedField]
    public FixedPoint2 MaxPain = 100;

    public FixedPoint2 MitigatedPercentage => FixedPoint2.Clamp(RawMitigatedPercentage, 0, 100);

    [DataField, AutoNetworkedField]
    public FixedPoint2 RawMitigatedPercentage = 0;

    public FixedPoint2 PainCap => FixedPoint2.Clamp(RawPainCap, 0, MaxPain);

    public FixedPoint2 MitigatedPain => RawPain * MitigatedPercentage / 100;

    public FixedPoint2 Pain => FixedPoint2.Clamp(RawPain* Multiplier - MitigatedPain, 0 , PainCap)  ;

    [NetSerializable, Serializable]
    [DataRecord]
    public record struct MedicalConditionThreshold(EntProtoId ConditionId, bool Applied);
}

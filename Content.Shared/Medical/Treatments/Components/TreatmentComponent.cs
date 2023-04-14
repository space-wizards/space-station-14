using Content.Shared.Medical.Treatments.Prototypes;
using Content.Shared.Medical.Treatments.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Medical.Treatments.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(TreatmentSystem))]
[AutoGenerateComponentState]
public sealed partial class TreatmentComponent : Component
{
    [DataField("treatmentType", required: true,
         customTypeSerializer: typeof(PrototypeIdSerializer<TreatmentTypePrototype>)), AutoNetworkedField]
    public string TreatmentType = "";

    [DataField("limitedUses"), AutoNetworkedField]
    public bool LimitedUses = true;

    [DataField("uses"), AutoNetworkedField]
    public int Uses = 1;

    [DataField("selfUsable"), AutoNetworkedField]
    public bool SelfUsable = true;

    [DataField("targetUsable"), AutoNetworkedField]
    public bool TargetUsable = true;
}

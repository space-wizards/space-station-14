using Content.Server.Medical.Treatments.Systems;
using Content.Shared.Medical.Treatments.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Medical.Treatments.Components;

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

    /// <summary>
    ///     How long it takes to apply the healing.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("delay")]
    public float Delay = 3f;

    /// <summary>
    ///     Delay multiplier when healing yourself.
    /// </summary>
    [DataField("selfHealPenaltyMultiplier")]
    public float SelfHealPenaltyMultiplier = 3f;

    /// <summary>
    ///     Sound played on healing begin
    /// </summary>
    [DataField("healingBeginSound"), AutoNetworkedField]
    public SoundSpecifier? HealingBeginSound = null;

    /// <summary>
    ///     Sound played on healing end
    /// </summary>
    [DataField("healingEndSound"), AutoNetworkedField]
    public SoundSpecifier? HealingEndSound = null;
}

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Offbrand.StatusEffects;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(CauseStatusEffectRandomlyStatusEffectSystem))]
public sealed partial class CauseStatusEffectRandomlyStatusEffectComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    [DataField(required: true)]
    public float Probability;

    [DataField(required: true)]
    public List<(EntProtoId, TimeSpan)> Effects;
}

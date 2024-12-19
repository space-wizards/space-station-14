using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Mally.Arkalyse.Components;

[RegisterComponent]
public sealed partial class ArkalyseMutedComponent : Component
{
    [DataField("timeMuted")]
    public float TimeMuted = 10.0f;

    [DataField("timeSuffocationStatus")]
    public float TimeSuffocation = 7.0f;

    [DataField("actionMutedAttackEntity")]
    public EntityUid? ActionMutedAttackEntity;

    [DataField("actionMutedAttack", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionMutedAttack = "ActionMutedAttack";

    [DataField("mutedAttack")]
    public bool IsMutedAttack = false;
}

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Audio;

namespace Content.Server.Mally.Arkalyse.Components;

[RegisterComponent]
public sealed partial class ArkalyseStunComponent : Component
{
    [DataField("paralyzetime")]
    public float ParalyzeTime = 1.5f;

    [DataField("actionStunAttackEntity")]
    public EntityUid? ActionStunAttackEntity;

    [DataField("actionStunAttack", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionStunAttack = "ActionStunAttack";

    [DataField("stunedAttack")]
    public bool IsStunedAttack = false;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("soundHit"), AutoNetworkedField]
    public SoundSpecifier HitSound = new SoundPathSpecifier("/Audio/Mally/sound_effects_hit_punch.ogg");
}

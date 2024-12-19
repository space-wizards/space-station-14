using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Audio;

namespace Content.Server.Mally.Arkalyse.Components;

[RegisterComponent]

public sealed partial class ArkalyseDamageComponent : Component
{
    [DataField("actionDamageAttackEntity")]
    public EntityUid? ActionDamageAttackEntity;

    [DataField("actionDamageAttack", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionDamageAttack = "ActionDamageAttack";

    [DataField("damageAttack")]
    public bool IsDamageAttack = false;

    [DataField("damageToface")]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new()
        {
            { "Piercing", 20 }
        }
    };

    [DataField("pushStrength")]
    public float PushStrength = 100f;

    [DataField("maxPushDistance")]
    public float MaxPushDistance = 2f;

    [DataField("distanceScaling")]
    public bool UseDistanceScaling = true;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("soundHit"), AutoNetworkedField]
    public SoundSpecifier HitSound = new SoundPathSpecifier("/Audio/Mally/sound_effects_hit_kick.ogg");
}

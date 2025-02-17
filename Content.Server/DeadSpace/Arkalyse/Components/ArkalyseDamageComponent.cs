// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Audio;

namespace Content.Server.DeadSpace.Arkalyse.Components;

[RegisterComponent]

public sealed partial class ArkalyseDamageComponent : Component
{
    [DataField]
    public EntityUid? ActionDamageArkalyseAttackEntity;

    [DataField("actionDamageArkalyseAttack", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionDamageArkalyseAttack = "ActionDamageArkalyseAttack";

    [DataField]
    public bool IsDamageAttack = false;

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new()
        {
            { "Piercing", 20 }
        }
    };

    [DataField]
    public float PushStrength = 100f;

    [DataField]
    public float MaxPushDistance = 2f;

    [DataField]
    public bool UseDistanceScaling = true;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField, AutoNetworkedField]
    public SoundSpecifier HitSound = new SoundPathSpecifier("/Audio/_DeadSpace/Arkalyse/sound_effects_hit_kick.ogg");
}

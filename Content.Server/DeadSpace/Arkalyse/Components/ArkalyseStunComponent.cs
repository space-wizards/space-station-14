// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Audio;

namespace Content.Server.DeadSpace.Arkalyse.Components;

[RegisterComponent]
public sealed partial class ArkalyseStunComponent : Component
{
    [DataField]
    public float ParalyzeTime = 1.5f;

    [DataField]
    public EntityUid? ActionStunArkalyseAttackEntity;

    [DataField("actionStunArkalyseAttack", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionStunArkalyseAttack = "ActionStunArkalyseAttack";

    [DataField]
    public bool IsStunedAttack = false;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField, AutoNetworkedField]
    public SoundSpecifier HitSound = new SoundPathSpecifier("/Audio/_DeadSpace/Arkalyse/sound_effects_hit_punch.ogg");
}

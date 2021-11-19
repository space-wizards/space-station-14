using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Wizard;

[RegisterComponent]
public class ImmovableRodComponent : Component
{
    public override string Name => "ImmovableRod";

    public int MobCount = 0;

    public EntityUid? Target = null;

    [DataField("hitSound")]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/bang.ogg");

    [DataField("hitSoundProbability")]
    public float HitSoundProbability = 0.1f;

    public float Accumulator = 0f;
}

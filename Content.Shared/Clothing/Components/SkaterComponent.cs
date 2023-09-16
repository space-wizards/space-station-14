using Robust.Shared.GameStates;
using Robust.Shared.Audio;

namespace Content.Shared.Clothing;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class SkaterComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float ParalyzeTime = 1f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float KnockChance = 0.04f;

    [DataField("knocksound")]
    public SoundSpecifier KnockSound = new SoundPathSpecifier("/Audio/Effects/slip.ogg");
}

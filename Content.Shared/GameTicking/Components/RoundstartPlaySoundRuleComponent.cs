using Content.Shared.GameTicking;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.GameTicking.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RoundstartPlaySoundRuleComponent : Component
{
    [DataField("sound", required: true)]
    public SoundSpecifier Sound = default!;

    [DataField("volume")]
    public float Volume = -8f;
}

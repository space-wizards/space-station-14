using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Cluwne;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedCluwneBrainSystem))]

public sealed class CluwneBrainComponent : Component
{
    [DataField("honkSound")]
    public SoundSpecifier HonkSound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");
}

using Robust.Shared.GameStates;
using Robust.Shared.Audio;

namespace Content.Shared.Cluwne;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedCluwneBrainSystem))]
public sealed class CluwneBrainComponent : Component
{
    [DataField("honkSound")]
    public SoundSpecifier HonkSound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");
}

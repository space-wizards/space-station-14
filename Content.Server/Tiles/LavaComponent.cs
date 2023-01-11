using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Server.Tiles;

/// <summary>
/// Applies flammable and damage while vaulting.
/// </summary>
[RegisterComponent, Access(typeof(LavaSystem))]
public sealed class LavaComponent : Component
{
    [DataField("soundDisintegration")]
    public SoundSpecifier DisintegrationSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");
}

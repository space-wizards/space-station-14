using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Server.Tiles;

/// <summary>
/// Applies flammable and damage while vaulting.
/// </summary>
[RegisterComponent, Access(typeof(LavaSystem))]
public sealed partial class LavaComponent : Component
{
    /// <summary>
    /// Sound played if something disintegrates in lava.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("soundDisintegration")]
    public SoundSpecifier DisintegrationSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");

    /// <summary>
    /// How many fire stacks are applied per second.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("fireStacks")]
    public float FireStacks = 1.25f;
}

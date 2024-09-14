using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Maps;

/// <summary>
/// This enables an entity to teleport with any navMap by pressing a key over the map screen
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NavMapWarpComponent : Component
{
    /// <summary>
    /// A random sound from this list plays on the client of the warping mob
    /// </summary>
    [DataField(required: true)]
    public SoundSpecifier Sounds = new SoundCollectionSpecifier("StaticShort");

    /// <summary>
    /// Random pitch variance. Set to 0 for no randomness;
    /// </summary>
    [DataField]
    public float PitchVariation = 0.1f;

}

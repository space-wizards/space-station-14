namespace Content.Shared.Maps;

/// <summary>
/// This enables an entity to teleport with any navMap by pressing a key over the map screen
/// </summary>
[RegisterComponent]
public sealed partial class NavMapWarpComponent : Component
{
    /// <summary>
    /// A random sound from this list plays on the client of the warping mob
    /// </summary>
    [DataField]
    public List<string> Sounds = new List<string>
    {
        "/Audio/Effects/static1.ogg",
        "/Audio/Effects/static2.ogg",
        "/Audio/Effects/static3.ogg"
    };

    /// <summary>
    /// Random pitch variance. Set to 0 for no randomness;
    /// </summary>
    [DataField]
    public float PitchVariation = 0.1f;

}

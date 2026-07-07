namespace Content.Server.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// Component for triggering node on getting activated by powerful magnets.
/// </summary>
[RegisterComponent, Access(typeof(XATMagnetSystem))]
public sealed partial class XATMagnetComponent : Component
{
    /// <summary>
    /// How close to the magnet do you have to be?
    /// </summary>
    [DataField]
    public float MagnetRange = 40f;

    /// <summary>
    /// How close do active magboots have to be?
    /// This is smaller because they are weaker magnets
    /// </summary>
    [DataField]
    public float MagbootsRange = 2f;
}

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;

/// <summary>
///     Activate artifact when it contacted with an electricity source.
///     It could be connected MV cables, stun baton or multi tool.
/// </summary>
[RegisterComponent]
public sealed partial class ArtifactElectricityTriggerComponent : Component
{
    /// <summary>
    ///     How much power should artifact receive to operate.
    /// </summary>
    [DataField("minPower")]
    public float MinPower = 400;
}

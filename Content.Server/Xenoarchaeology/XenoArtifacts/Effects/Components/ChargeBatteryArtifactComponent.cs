namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
/// This is used for recharging all nearby batteries when activated
/// </summary>
[RegisterComponent]
public sealed class ChargeBatteryArtifactComponent : Component
{
    /// <summary>
    /// The radius of entities that will be affected
    /// </summary>
    [DataField("radius")]
    public float Radius = 15f;
}

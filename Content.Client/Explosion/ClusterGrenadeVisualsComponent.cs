using Content.Shared.Explosion.EntitySystems;
namespace Content.Client.Explosion;

/// <summary>
/// The Visual Component for the Cluster Grenade.
/// <see cref="ScatteringGrenadeSystem"/>
/// </summary>
[RegisterComponent]
[Access(typeof(ClusterGrenadeVisualizerSystem))]
public sealed partial class ClusterGrenadeVisualsComponent : Component
{
    /// <summary>
    /// Used to select the correct layer from the rsi together with the grenade amount.
    /// <see cref="ClusterGrenadeVisualizerSystem.OnAppearanceChange"/>
    /// </summary>
    [DataField("state")]
    public string? State;
}

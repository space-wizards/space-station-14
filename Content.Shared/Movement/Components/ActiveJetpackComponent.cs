using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Added to an enabled and flying jetpack. Tracked separately on server/client to know when to start handling gas usage on server, and spawn flying particles on client.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveJetpackComponent : Component
{
    [DataField]
    public TimeSpan EffectCooldown = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// Max distance from <see cref="LastCoordinates"/> that this jetpack must be to emit particles.
    /// </summary>
    [DataField]
    public float MaxDistance = 0.7f;

    /// <summary>
    /// The last position at which particles were emitted from this jetpack.
    /// </summary>
    [DataField]
    public EntityCoordinates? LastCoordinates = null;

    /// <summary>
    /// The time after which particles can be emitted from this jetpack.
    /// </summary>
    [DataField]
    public TimeSpan TargetTime = TimeSpan.MinValue;
}

using Content.Server.StationEvents.Events;
using Content.Shared.Storage;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(ImmovableRodRule))]
public sealed partial class ImmovableRodRuleComponent : Component
{
    /// <summary>
    ///     List of possible rods and spawn probabilities.
    /// </summary>
    [DataField]
    public List<EntitySpawnEntry> RodPrototypes = new();

    /// <summary>
    ///     How fast the rods are fired at those idiots on their dumb space station.
    /// </summary>
    [DataField]
    public float LaunchSpeed = 25.0f;

    /// <summary>
    ///     How many seconds will it take for the rod to hit the target location?
    /// </summary>
    public TimeSpan PreTargetLifespan = TimeSpan.FromSeconds(15);

    /// <summary>
    ///     How many seconds each spawned rod will live for after colliding with the target location.
    /// </summary>
    [DataField]
    public TimeSpan PostTargetLifespan = TimeSpan.FromSeconds(15);

    public float TotalLifespanSeconds => PreTargetLifespan.Seconds + PostTargetLifespan.Seconds;
}

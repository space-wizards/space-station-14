using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(ImmovableRodRule))]
public sealed class ImmovableRodRuleComponent : Component
{
    [DataField("minSpeed")]
    public float MinSpeed = 10f;

    [DataField("maxSpeed")]
    public float MaxSpeed = 40f;

    /// <summary>
    /// Rod lifetime in seconds.
    /// </summary>
    [DataField("lifetime")]
    public float Lifetime = 60f;

    /// <summary>
    /// With this set to true, rods will automatically set the tiles under them to space.
    /// </summary>
    [DataField("destroyTiles")]
    public bool DestroyTiles = false;
}

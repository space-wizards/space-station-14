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
}

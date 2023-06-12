using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(ImmovableRodRule))]
public sealed class ImmovableRodRuleComponent : Component
{
    [DataField("minSpeed")]
    public float minSpeed = 10f;

    [DataField("maxSpeed")]
    public float maxSpeed = 40f;

    [DataField("lifetime")]
    public float lifetime = 60f;
}

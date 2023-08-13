using Content.Server.Power.EntitySystems;

namespace Content.Server.Power.Components;

[RegisterComponent]
[Access(typeof(BreakerSystem))]
public sealed class BreakerComponent : Component
{
    /// <summary>
    /// Once power supplied exceeds this limit the breaker will pop.
    /// </summary>
    [DataField("limit", required: true), ViewVariables(VVAccess.ReadWrite)]
    public float Limit;
}

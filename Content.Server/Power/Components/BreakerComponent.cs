using Content.Server.Power.EntitySystems;

namespace Content.Server.Power.Components;

[RegisterComponent]
[Access(typeof(BreakerSystem))]
public sealed class BreakerComponent : Component
{
    /// <summary>
    /// Once power supplied exceeds this limit the breaker will start to pop.
    /// </summary>
    [DataField("limit", required: true), ViewVariables(VVAccess.ReadWrite)]
    public float Limit;

    /// <summary>
    /// Time that supply must exceed limit for the breaker to pop.
    /// </summary>
    [DataField("popTime"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan PopTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Whether the breaker is starting to pop.
    /// </summary>
    [ViewVariables]
    public bool Overloaded;

    /// <summary>
    /// When the breaker will pop.
    /// Set when supply first exceeds limit.
    /// </summary>
    [ViewVariables]
    public TimeSpan NextPop;
}

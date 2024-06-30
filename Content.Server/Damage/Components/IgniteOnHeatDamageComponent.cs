using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Server.Damage.Components;

[RegisterComponent]
public sealed partial class IgniteOnHeatDamageComponent : Component
{
    [DataField]
    public float FireStacks = 1f;

    /// <summary>
    /// The minimum amount of damage taken to ignite and apply fire stacks
    /// </summary>
    [DataField]
    public FixedPoint2 Threshold = 15;

    /// <summary>
    /// If you're properly hydrated, you won't ignite on hit
    /// but it will decrease your hydration by the
    /// amount of damage you take multipled by the
    /// <see cref="IgniteOnHeatDamageComponent.DehydrationMultiplier"/>
    ///
    /// And you will then ignite when completely dehydrated.
    /// </summary>
    [DataField]
    public bool PreventedByHydration = false;

    [DataField]
    public float DehydrationMultiplier = 1f;
}

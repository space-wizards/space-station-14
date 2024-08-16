using Content.Shared.FixedPoint;

namespace Content.Server.Damage.Components;

[RegisterComponent]
public sealed partial class IgniteOnHeatDamageComponent : Component
{
    [DataField]
    public float FireStacks = 1f;

    // The minimum amount of damage taken to apply fire stacks
    [DataField]
    public FixedPoint2 Threshold = 15;

    /// <summary>
    /// If you're properly hydrated, you won't ignite on hit
    /// Your hydration will decrease by the amount of damage you take multiplied by
    /// <see cref="IgniteOnHeatDamageComponent.DehydrationMultiplier"/>
    /// And you will then ignite when completely dehydrated.
    /// </summary>
    [DataField]
    public bool PreventedByHydration;

    [DataField]
    public float DehydrationMultiplier = 1f;
}

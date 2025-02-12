using Content.Shared.Damage;
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
}

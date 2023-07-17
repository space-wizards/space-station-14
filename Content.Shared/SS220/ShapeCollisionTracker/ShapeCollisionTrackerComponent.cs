using System.Collections.Immutable;
using Robust.Shared.Physics.Collision.Shapes;

namespace Content.Shared.SS220.ShapeCollisionTracker;

[RegisterComponent]
public sealed class ShapeCollisionTrackerComponent : Component
{
    public const string FixtureID  = "collision-tracker-fixture";

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("enabled")]
    public bool Enabled = true;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("requireAnchored")]
    public bool RequiresAnchored = true;

    public readonly HashSet<EntityUid> Colliding = new();

    [DataField("shape", required: true)]
    public IPhysShape Shape { get; set; } = new PhysShapeCircle(2f);
}

public sealed class ShapeCollisionTrackerUpdatedEvent : EntityEventArgs
{
    public readonly ImmutableHashSet<EntityUid> Colliding;

    public ShapeCollisionTrackerUpdatedEvent(ImmutableHashSet<EntityUid> colliding)
    {
        Colliding = colliding;
    }
}

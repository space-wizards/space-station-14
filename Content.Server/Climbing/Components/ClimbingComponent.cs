using Content.Shared.Climbing;

namespace Content.Server.Climbing.Components;

[RegisterComponent]
[ComponentReference(typeof(SharedClimbingComponent))]
[Access(typeof(ClimbSystem))]
public sealed class ClimbingComponent : SharedClimbingComponent
{
    [ViewVariables(VVAccess.ReadWrite)]
    public override bool IsClimbing
    {
        get => base.IsClimbing;
        set
        {
            if (base.IsClimbing == value) return;
            base.IsClimbing = value;
            Dirty();
        }
    }

    public override bool OwnerIsTransitioning
    {
        get => base.OwnerIsTransitioning;
        set
        {
            if (value == base.OwnerIsTransitioning) return;
            base.OwnerIsTransitioning = value;
            Dirty();
        }
    }

    [ViewVariables]
    public Dictionary<string, int> DisabledFixtureMasks { get; } = new();
}

using Content.Shared.Climbing;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Climbing.Components;

[RegisterComponent]
[ComponentReference(typeof(SharedClimbingComponent))]
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
    public List<Fixture> DisabledFixtures { get; } = new();

    [ViewVariables]
    public List<string> Fixtures { get; } = new();

    public override ComponentState GetComponentState()
    {
        return new ClimbModeComponentState(base.IsClimbing, OwnerIsTransitioning);
    }
}

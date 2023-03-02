using Content.Shared.Movement.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Movement.Systems;

public sealed class SlowContactsSystem : VirtualController
{
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifierSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesAfter.Add(typeof(SharedMoverController));
        SubscribeLocalEvent<SlowContactsComponent, StartCollideEvent>(OnEntityEnter);
        SubscribeLocalEvent<SlowContactsComponent, EndCollideEvent>(OnEntityExit);
        SubscribeLocalEvent<SlowedByContactComponent, RefreshMovementSpeedModifiersEvent>(MovementSpeedCheck);

        SubscribeLocalEvent<SlowContactsComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<SlowContactsComponent, ComponentGetState>(OnGetState);
    }

    private void OnGetState(EntityUid uid, SlowContactsComponent component, ref ComponentGetState args)
    {
        args.State = new SlowContactsComponentState(component.WalkSpeedModifier, component.SprintSpeedModifier);
    }

    private void OnHandleState(EntityUid uid, SlowContactsComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not SlowContactsComponentState state)
            return;

        component.WalkSpeedModifier = state.WalkSpeedModifier;
        component.SprintSpeedModifier = state.SprintSpeedModifier;
    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);

        var query = EntityQueryEnumerator<SlowedByContactComponent, MovementSpeedModifierComponent>();

        // TODO: I really hate this but I have tried for an hour to fix the mispredict yet somehow
        // it still felt worse than conveyors.
        // It may just be the contact mispredict but that requires significant physics work + optimisation +
        // box2d 3.0 is coming so all of it may get uplifted.
        while (query.MoveNext(out var uid, out _, out var modifier))
        {
            _speedModifierSystem.RefreshMovementSpeedModifiers(uid, modifier);
        }
    }

    private void MovementSpeedCheck(EntityUid uid, SlowedByContactComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        var walkSpeed = 1.0f;
        var sprintSpeed = 1.0f;

        foreach (var ent in component.Intersecting)
        {
            if (!TryComp<SlowContactsComponent>(ent, out var slowContactsComponent))
                continue;

            if (slowContactsComponent.IgnoreWhitelist != null && slowContactsComponent.IgnoreWhitelist.IsValid(uid))
                continue;

            walkSpeed = Math.Min(walkSpeed, slowContactsComponent.WalkSpeedModifier);
            sprintSpeed = Math.Min(sprintSpeed, slowContactsComponent.SprintSpeedModifier);
        }

        args.ModifySpeed(walkSpeed, sprintSpeed);
    }

    private void OnEntityExit(EntityUid uid, SlowContactsComponent component, ref EndCollideEvent args)
    {
        var otherUid = args.OtherFixture.Body.Owner;

        if (!TryComp<SlowedByContactComponent>(otherUid, out var slowed))
        {
            return;
        }

        slowed.Intersecting.Remove(uid);
    }

    private void OnEntityEnter(EntityUid uid, SlowContactsComponent component, ref StartCollideEvent args)
    {
        var otherUid = args.OtherFixture.Body.Owner;
        if (!HasComp<MovementSpeedModifierComponent>(otherUid) ||
            !TryComp<SlowedByContactComponent>(otherUid, out var slowed))
        {
            return;
        }

        slowed.Intersecting.Add(uid);
    }
}

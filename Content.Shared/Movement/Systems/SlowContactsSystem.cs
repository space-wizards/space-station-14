using Content.Shared.Movement.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Events;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement.Systems;

public sealed class SlowContactsSystem : VirtualController
{
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifierSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesBefore.Add(typeof(SharedMoverController));
        SubscribeLocalEvent<SlowContactsComponent, StartCollideEvent>(OnEntityEnter);
        SubscribeLocalEvent<SlowContactsComponent, EndCollideEvent>(OnEntityExit);
        SubscribeLocalEvent<SlowContactsComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<SlowContactsComponent, ComponentGetState>(OnGetState);

        SubscribeLocalEvent<SlowedByContactComponent, RefreshMovementSpeedModifiersEvent>(MovementSpeedCheck);
        SubscribeLocalEvent<SlowedByContactComponent, ComponentGetState>(OnSlowedGetState);
        SubscribeLocalEvent<SlowedByContactComponent, ComponentHandleState>(OnSlowedHandleState);
    }

    private void OnSlowedHandleState(EntityUid uid, SlowedByContactComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not SlowedByContactComponentState state)
            return;

        component.Refresh = state.Active;
    }

    [Serializable, NetSerializable]
    private sealed class SlowedByContactComponentState : ComponentState
    {
        public readonly bool Active;

        public SlowedByContactComponentState(bool active)
        {
            Active = active;
        }
    }

    private void OnSlowedGetState(EntityUid uid, SlowedByContactComponent component, ref ComponentGetState args)
    {
        args.State = new SlowedByContactComponentState(component.Refresh);
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

        var query = EntityQueryEnumerator<SlowedByContactComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            // TODO: Contacts not working with prediction
            //if (!comp.Refresh)
            //    continue;

            _speedModifierSystem.RefreshMovementSpeedModifiers(uid);
            //comp.Refresh = false;
            //Dirty(comp);
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

        if (!TryComp<SlowedByContactComponent>(otherUid, out var slowed) ||
            slowed.Fixture != args.OtherFixture.ID ||
            !slowed.Intersecting.Remove(uid))
        {
            return;
        }

        //slowed.Refresh = true;
        //Dirty(slowed);
    }

    private void OnEntityEnter(EntityUid uid, SlowContactsComponent component, ref StartCollideEvent args)
    {
        var otherUid = args.OtherFixture.Body.Owner;
        if (!TryComp<SlowedByContactComponent>(otherUid, out var slowed) ||
            slowed.Fixture != args.OtherFixture.ID ||
            !HasComp<MovementSpeedModifierComponent>(otherUid) ||
            !slowed.Intersecting.Add(uid))
        {
            return;
        }

        //slowed.Refresh = true;
        //Dirty(slowed);
    }
}

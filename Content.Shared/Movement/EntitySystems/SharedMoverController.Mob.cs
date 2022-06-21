using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Movement.EntitySystems;

public abstract partial class SharedMoverController
{
    private void InitializeMob()
    {
        SubscribeLocalEvent<MobMoverComponent, ComponentGetState>(OnMobGetState);
        SubscribeLocalEvent<MobMoverComponent, ComponentHandleState>(OnMobHandleState);
        SubscribeLocalEvent<MobMoverComponent, ComponentInit>(OnMobInit);
    }

    private void OnMobInit(EntityUid uid, MobMoverComponent component, ComponentInit args)
    {
        component.LastGridAngle = Transform(uid).Parent?.WorldRotation ?? new Angle(0);
    }

    private void OnMobHandleState(EntityUid uid, MobMoverComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MobMoverComponentState state) return;
        component.GrabRange = state.GrabRange;
        component.PushStrength = state.PushStrength;
        component.BaseWalkSpeed = state.BaseWalkSpeed;
        component.BaseSprintSpeed = state.BaseSprintSpeed;
        component.WalkSpeedModifier = state.WalkSpeedModifier;
        component.SprintSpeedModifier = state.SprintSpeedModifier;
        component._heldMoveButtons = state.Buttons;
        component._lastInputTick = GameTick.Zero;
        component._lastInputSubTick = 0;
        component.CanMove = state.CanMove;
    }

    private void OnMobGetState(EntityUid uid, MobMoverComponent component, ref ComponentGetState args)
    {
        args.State = new MobMoverComponentState(
            component.GrabRange,
            component.PushStrength,
            component.BaseWalkSpeed,
            component.BaseSprintSpeed,
            component.WalkSpeedModifier,
            component.SprintSpeedModifier);
    }

    #region Movement Speed Modifiers

    public void RefreshMovementSpeedModifiers(EntityUid uid, MobMoverComponent? move = null)
    {
        if (!Resolve(uid, ref move, false))
            return;

        var ev = new RefreshMovementSpeedModifiersEvent();
        RaiseLocalEvent(uid, ev, false);

        if (move.WalkSpeedModifier.Equals(ev.WalkSpeedModifier) &&
            move.SprintSpeedModifier.Equals(ev.SprintSpeedModifier)) return;

        move.WalkSpeedModifier = ev.WalkSpeedModifier;
        move.SprintSpeedModifier = ev.SprintSpeedModifier;

        Dirty(move);
    }

    #endregion

    [Serializable, NetSerializable]
    protected sealed class MobMoverComponentState : ComponentState
    {
        public float GrabRange;
        public float PushStrength;
        public float BaseWalkSpeed;
        public float BaseSprintSpeed;
        public float WalkSpeedModifier;
        public float SprintSpeedModifier;
        public MobMoverComponent.MoveButtons Buttons { get; }
        public readonly bool CanMove;

        public MobMoverComponentState(
            float grabRange,
            float pushStrength,
            float baseWalkSpeed,
            float baseSprintSpeed,
            float walkSpeedModifier,
            float sprintSpeedModifier)
        {
            GrabRange = grabRange;
            PushStrength = pushStrength;
            BaseWalkSpeed = baseWalkSpeed;
            BaseSprintSpeed = baseSprintSpeed;
            WalkSpeedModifier = walkSpeedModifier;
            SprintSpeedModifier = sprintSpeedModifier;
        }
    }
}

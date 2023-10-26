using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Movement.Systems
{
    public sealed class MovementSpeedModifierSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;

        public void RefreshMovementSpeedModifiers(EntityUid uid, MovementSpeedModifierComponent? move = null)
        {
            if (!Resolve(uid, ref move, false))
                return;

            if (_timing.ApplyingState)
                return;

            var ev = new RefreshMovementSpeedModifiersEvent();
            RaiseLocalEvent(uid, ev);

            if (MathHelper.CloseTo(ev.WalkSpeedModifier, move.WalkSpeedModifier) &&
                MathHelper.CloseTo(ev.SprintSpeedModifier, move.SprintSpeedModifier) &&
                MathHelper.CloseTo(ev.Friction, move.Friction) &&
                MathHelper.CloseTo(ev.Acceleration, move.Acceleration))
                return;

            move.WalkSpeedModifier = ev.WalkSpeedModifier;
            move.SprintSpeedModifier = ev.SprintSpeedModifier;
            move.Friction = ev.Friction;
            move.FrictionNoInput = ev.FrictionNoInput;
            move.Acceleration = ev.Acceleration;
            Dirty(uid, move);
        }

        public void ChangeBaseSpeed(EntityUid uid, float baseWalkSpeed, float baseSprintSpeed, float acceleration, MovementSpeedModifierComponent? move = null)
        {
            if (!Resolve(uid, ref move, false))
                return;

            move.BaseWalkSpeed = baseWalkSpeed;
            move.BaseSprintSpeed = baseSprintSpeed;
            move.Acceleration = acceleration;
            Dirty(uid, move);
        }
    }

    /// <summary>
    ///     Raised on an entity to determine its new movement speed. Any system that wishes to change movement speed
    ///     should hook into this event and set it then. If you want this event to be raised,
    ///     call <see cref="MovementSpeedModifierSystem.RefreshMovementSpeedModifiers"/>.
    /// </summary>
    public sealed class RefreshMovementSpeedModifiersEvent : EntityEventArgs, IInventoryRelayEvent
    {
        public SlotFlags TargetSlots { get; } = ~SlotFlags.POCKET;

        public float WalkSpeedModifier { get; private set; } = 1.0f;
        public float SprintSpeedModifier { get; private set; } = 1.0f;
        public float Friction { get; private set; } = MovementSpeedModifierComponent.DefaultFriction;
        public float? FrictionNoInput { get; private set; } = null;
        public float Acceleration { get; private set; } = MovementSpeedModifierComponent.DefaultAcceleration;

        public void ModifySpeed(float walk, float sprint)
        {
            WalkSpeedModifier *= walk;
            SprintSpeedModifier *= sprint;
        }

        public void ChangeFriction(float friction, float? frictionNoInput, float acceleration)
        {
            Friction = friction;
            FrictionNoInput = frictionNoInput;
            Acceleration = acceleration;
        }
    }
}

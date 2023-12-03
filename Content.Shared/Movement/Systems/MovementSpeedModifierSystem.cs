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
                MathHelper.CloseTo(ev.WeightlessModifier, move.WeightlessMultiplier))
                return;

            move.WalkSpeedModifier = ev.WalkSpeedModifier;
            move.SprintSpeedModifier = ev.SprintSpeedModifier;
            move.WeightlessMultiplier = ev.WeightlessModifier;
            Dirty(uid, move);
        }

        public void RefreshMovementAccelerationModifiers(EntityUid uid, MovementSpeedModifierComponent? move = null)
        {
            if (!Resolve(uid, ref move))
                return;

            if (_timing.ApplyingState)
                return;

            var ev = new RefreshMovementAccelerationModifiersEvent();
            RaiseLocalEvent(uid, ev);

            if (MathHelper.CloseTo(ev.AccelerationModifier, move.AccelerationModifier) &&
                MathHelper.CloseTo(ev.WeightlessAccelerationModifier, move.WeightlessAccelerationModifier))
                return;

            move.AccelerationModifier = ev.AccelerationModifier;
            move.WeightlessAccelerationModifier = ev.WeightlessAccelerationModifier;

            Dirty(uid, move);
        }

        public void RefreshMovementFrictionModifiers(EntityUid uid, MovementSpeedModifierComponent? move = null)
        {
            if (!Resolve(uid, ref move))
                return;

            if (_timing.ApplyingState)
                return;

            var ev = new RefreshMovementFrictionModifiersEvent();
            RaiseLocalEvent(uid, ev);

            if (MathHelper.CloseTo(ev.FrictionModifier, move.FrictionModifier) &&
                MathHelper.CloseTo(ev.FrictionNoInputModifier, move.FrictionNoInputModifier) &&
                MathHelper.CloseTo(ev.WeightlessFrictionModifier, move.WeightlessFrictionModifier) &&
                MathHelper.CloseTo(ev.WeightlessFrictionNoInputModifier, move.WeightlessFrictionNoInputModifier))
                return;

            move.FrictionModifier = ev.FrictionModifier;
            move.FrictionNoInputModifier = ev.FrictionNoInputModifier;
            move.WeightlessFrictionModifier = ev.WeightlessFrictionModifier;
            move.WeightlessFrictionNoInputModifier = ev.WeightlessFrictionNoInputModifier;

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

        public void ChangeFriction(EntityUid uid, float friction, float? frictionNoInput, float acceleration, MovementSpeedModifierComponent? move = null)
        {
            if (!Resolve(uid, ref move, false))
                return;

            move.Friction = friction;
            move.FrictionNoInput = frictionNoInput;
            move.Acceleration = acceleration;
            Dirty(uid, move);
        }

        /// <summary>
        /// Allows the changing of weightless speed & acceleration.
        /// </summary>
        public void ChangeWeightlessSpeed(EntityUid uid, float weightlessModifier, float weightlessAcceleration, MovementSpeedModifierComponent? move = null)
        {
            if (!Resolve(uid, ref move))
                return;

            move.WeightlessModifier = weightlessModifier;
            move.WeightlessAcceleration = weightlessAcceleration;

            Dirty(uid, move);
        }

        /// <summary>
        /// Allows the changing of weightless friction
        /// </summary>
        public void ChangeWeightlessFriction(EntityUid uid, float weightlessFriction, float weightlessFrictionNoInput, MovementSpeedModifierComponent? move = null)
        {
            if (!Resolve(uid, ref move))
                return;

            move.WeightlessFriction = weightlessFriction;
            move.WeightlessFrictionNoInput = weightlessFrictionNoInput;

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
        public float WeightlessModifier { get; private set; } = 1.0f;

        public void ModifySpeed(float walk, float sprint)
        {
            WalkSpeedModifier *= walk;
            SprintSpeedModifier *= sprint;
        }

        public void ModifyWeightless(float modifier)
        {
            WeightlessModifier *= modifier;
        }
    }

    /// <summary>
    /// Raised on an entity to determine it's new acceleration. Any system that wants to change acceleration hook into this event and set it there.
    /// </summary>
    /// <remarks>
    /// If you want to raise the event, call <see cref="MovementSpeedModifierSystem.RefreshMovementAccelerationModifiers"/>
    /// </remarks>
    public sealed class RefreshMovementAccelerationModifiersEvent : EntityEventArgs, IInventoryRelayEvent
    {
        public SlotFlags TargetSlots { get; } = ~SlotFlags.POCKET;

        public float AccelerationModifier { get; private set; } = 1.0f;
        public float WeightlessAccelerationModifier { get; private set; } = 1.0f;

        public void Modify(float acceleration)
        {
            AccelerationModifier *= acceleration;
        }

        public void ModifyWeightless(float acceleration)
        {
            WeightlessAccelerationModifier *= acceleration;
        }
    }

    /// <summary>
    /// Raised on an entity to determine it's new friction. Any system that wants to change friction hook into this event and set it there.
    /// </summary>
    /// <remarks>
    /// If you want to raise the event, call <see cref="MovementSpeedModifierSystem.RefreshMovementFrictionModifiers"/>
    /// </remarks>
    public sealed class RefreshMovementFrictionModifiersEvent : EntityEventArgs, IInventoryRelayEvent
    {
        public SlotFlags TargetSlots { get; } = ~SlotFlags.POCKET;

        public float FrictionModifier { get; private set; } = 1.0f;
        public float FrictionNoInputModifier { get; private set; } = 1.0f;
        public float WeightlessFrictionModifier { get; private set; } = 1.0f;
        public float WeightlessFrictionNoInputModifier { get; private set; } = 1.0f;

        public void Modify(float friction, float noInputFriction)
        {
            FrictionModifier *= friction;
            FrictionNoInputModifier *= noInputFriction;
        }

        public void ModifyWeightless(float friction, float noInputFriction)
        {
            WeightlessFrictionModifier *= friction;
            WeightlessFrictionNoInputModifier *= noInputFriction;
        }
    }
}

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
                MathHelper.CloseTo(ev.WeightlessModifier, move.WeightlessMultiplier) &&
                MathHelper.CloseTo(ev.AccelerationModifier, move.AccelerationModifier) &&
                MathHelper.CloseTo(ev.WeightlessAccelerationModifier, move.WeightlessAccelerationModifier) &&
                MathHelper.CloseTo(ev.FrictionModifier, move.FrictionModifier) &&
                MathHelper.CloseTo(ev.FrictionNoInputModifier, move.FrictionNoInputModifier) &&
                MathHelper.CloseTo(ev.WeightlessFrictionModifier, move.WeightlessFrictionModifier) &&
                MathHelper.CloseTo(ev.WeightlessFrictionNoInputModifier, move.WeightlessFrictionNoInputModifier))
                return;

            move.WalkSpeedModifier = ev.WalkSpeedModifier;
            move.SprintSpeedModifier = ev.SprintSpeedModifier;
            move.WeightlessMultiplier = ev.WeightlessModifier;
            move.AccelerationModifier = ev.AccelerationModifier;
            move.WeightlessAccelerationModifier = ev.WeightlessAccelerationModifier;
            move.FrictionModifier = ev.FrictionModifier;
            move.FrictionNoInputModifier = ev.FrictionNoInputModifier;
            move.WeightlessFrictionModifier = ev.WeightlessFrictionModifier;
            move.WeightlessFrictionNoInputModifier = ev.WeightlessFrictionNoInputModifier;

            Dirty(uid, move);
        }

    }

    /// <summary>
    ///     Raised on an entity to determine its new movement speed, acceleration and friction. Any system that wishes to change movement speed
    ///     should hook into this event and set it then. If you want this event to be raised,
    ///     call <see cref="MovementSpeedModifierSystem.RefreshMovementSpeedModifiers"/>.
    /// </summary>
    public sealed class RefreshMovementSpeedModifiersEvent : EntityEventArgs, IInventoryRelayEvent
    {
        public SlotFlags TargetSlots { get; } = ~SlotFlags.POCKET;

        public float WalkSpeedModifier { get; private set; } = 1.0f;
        public float SprintSpeedModifier { get; private set; } = 1.0f;
        public float WeightlessModifier { get; private set; } = 1.0f;
        public float AccelerationModifier { get; private set; } = 1.0f;
        public float WeightlessAccelerationModifier { get; private set; } = 1.0f;
        public float FrictionModifier { get; private set; } = 1.0f;
        public float FrictionNoInputModifier { get; private set; } = 1.0f;
        public float WeightlessFrictionModifier { get; private set; } = 1.0f;
        public float WeightlessFrictionNoInputModifier { get; private set; } = 1.0f;

        public void ModifySpeed(float walk, float sprint)
        {
            WalkSpeedModifier *= walk;
            SprintSpeedModifier *= sprint;
        }

        public void ModifyWeightlessModifier(float modifier)
        {
            WeightlessModifier *= modifier;
        }

        public void ModifyAcceleration(float acceleration)
        {
            AccelerationModifier *= acceleration;
        }

        public void ModifyWeightlessAcceleration(float acceleration)
        {
            WeightlessAccelerationModifier *= acceleration;
        }

        public void ModifyFriction(float friction, float noInputFriction)
        {
            FrictionModifier *= friction;
            FrictionNoInputModifier *= noInputFriction;
        }

        public void ModifyWeightlessFriction(float friction, float noInputFriction)
        {
            WeightlessFrictionModifier *= friction;
            WeightlessFrictionNoInputModifier *= noInputFriction;
        }
    }
}

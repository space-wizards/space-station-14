using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Movement.Systems
{
    public sealed class MovementSpeedModifierSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MovementSpeedModifierComponent, MapInitEvent>(OnModMapInit);
        }

        private void OnModMapInit(Entity<MovementSpeedModifierComponent> ent, ref MapInitEvent args)
        {
            // TODO: Dirty these smarter.
            ent.Comp.WeightlessAcceleration = ent.Comp.BaseWeightlessAcceleration;
            ent.Comp.WeightlessModifier = ent.Comp.BaseWeightlessModifier;
            ent.Comp.WeightlessFriction = ent.Comp.BaseWeightlessFriction;
            ent.Comp.WeightlessFrictionNoInput = ent.Comp.BaseWeightlessFrictionNoInput;
            Dirty(ent);
        }

        public void RefreshWeightlessModifiers(EntityUid uid, MovementSpeedModifierComponent? move = null)
        {
            if (!Resolve(uid, ref move, false))
                return;

            if (_timing.ApplyingState)
                return;

            var ev = new RefreshWeightlessModifiersEvent()
            {
                WeightlessAcceleration = move.BaseWeightlessAcceleration,
                WeightlessModifier = move.BaseWeightlessModifier,
                WeightlessFriction = move.BaseWeightlessFriction,
                WeightlessFrictionNoInput = move.BaseWeightlessFrictionNoInput,
            };

            RaiseLocalEvent(uid, ref ev);

            if (MathHelper.CloseTo(ev.WeightlessAcceleration, move.WeightlessAcceleration) &&
                MathHelper.CloseTo(ev.WeightlessModifier, move.WeightlessModifier) &&
                MathHelper.CloseTo(ev.WeightlessFriction, move.WeightlessFriction))
            {
                return;
            }

            move.WeightlessAcceleration = ev.WeightlessAcceleration;
            move.WeightlessModifier = ev.WeightlessModifier;
            move.WeightlessFriction = ev.WeightlessFriction;
            Dirty(uid, move);
        }

        public void RefreshMovementSpeedModifiers(EntityUid uid, MovementSpeedModifierComponent? move = null)
        {
            if (!Resolve(uid, ref move, false))
                return;

            if (_timing.ApplyingState)
                return;

            var ev = new RefreshMovementSpeedModifiersEvent();
            RaiseLocalEvent(uid, ev);

            if (MathHelper.CloseTo(ev.WalkSpeedModifier, move.WalkSpeedModifier) &&
                MathHelper.CloseTo(ev.SprintSpeedModifier, move.SprintSpeedModifier))
                return;

            move.WalkSpeedModifier = ev.WalkSpeedModifier;
            move.SprintSpeedModifier = ev.SprintSpeedModifier;
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

        // We might want to create separate RefreshMovementFrictionModifiersEvent and RefreshMovementFrictionModifiers function that will call it
        public void ChangeFriction(EntityUid uid, float friction, float? frictionNoInput, float acceleration, MovementSpeedModifierComponent? move = null)
        {
            if (!Resolve(uid, ref move, false))
                return;

            move.Friction = friction;
            move.FrictionNoInput = frictionNoInput;
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

        public void ModifySpeed(float walk, float sprint)
        {
            WalkSpeedModifier *= walk;
            SprintSpeedModifier *= sprint;
        }

        public void ModifySpeed(float mod)
        {
            ModifySpeed(mod, mod);
        }
    }

    [ByRefEvent]
    public record struct RefreshWeightlessModifiersEvent()
    {
        public float WeightlessAcceleration;

        public float WeightlessFriction;

        public float WeightlessModifier;

        public float WeightlessFrictionNoInput;
    }
}

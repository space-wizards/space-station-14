using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
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
            SubscribeLocalEvent<MovementSpeedModifierComponent, TileFrictionEvent>(OnTileFriction);
        }

        private void OnModMapInit(Entity<MovementSpeedModifierComponent> ent, ref MapInitEvent args)
        {
            // TODO: Dirty these smarter.
            ent.Comp.WeightlessAcceleration = ent.Comp.BaseWeightlessAcceleration;
            ent.Comp.WeightlessModifier = ent.Comp.BaseWeightlessModifier;
            ent.Comp.WeightlessFriction = ent.Comp.BaseWeightlessFriction;
            ent.Comp.WeightlessFrictionNoInput = ent.Comp.BaseWeightlessFrictionNoInput;
            ent.Comp.Friction = ent.Comp.BaseFriction;
            ent.Comp.Acceleration = ent.Comp.BaseAcceleration;
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
                MathHelper.CloseTo(ev.WeightlessFriction, move.WeightlessFriction) &&
                MathHelper.CloseTo(ev.WeightlessFrictionNoInput, move.WeightlessFrictionNoInput))
            {
                return;
            }

            move.WeightlessAcceleration = ev.WeightlessAcceleration;
            move.WeightlessModifier = ev.WeightlessModifier;
            move.WeightlessFriction = ev.WeightlessFriction;
            move.WeightlessFrictionNoInput = ev.WeightlessFrictionNoInput;
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

        public void RefreshFrictionModifiers(EntityUid uid, MovementSpeedModifierComponent? move = null)
        {
            if (!Resolve(uid, ref move, false))
                return;

            if (_timing.ApplyingState)
                return;

            var ev = new RefreshFrictionModifiersEvent()
            {
                Friction = move.BaseFriction,
                Acceleration = move.BaseAcceleration
            };
            RaiseLocalEvent(uid, ref ev);

            if (MathHelper.CloseTo(ev.Friction, move.Friction) && ev.FrictionNoInput == null && ev.Acceleration == null)
                return;

            move.Friction = ev.Friction;
            move.FrictionNoInput = ev.FrictionNoInput * move.BaseFriction;
            move.Acceleration = ev.Acceleration;

            Dirty(uid, move);
        }

        public void ChangeBaseFriction(EntityUid uid, float friction, float? frictionNoInput, float acceleration, MovementSpeedModifierComponent? move = null)
        {
            if (!Resolve(uid, ref move, false))
                return;

            move.BaseFriction = friction;
            move.FrictionNoInput = frictionNoInput;
            move.BaseAcceleration = acceleration;
            Dirty(uid, move);
        }

        private void OnTileFriction(Entity<MovementSpeedModifierComponent> ent, ref TileFrictionEvent args)
        {
            args.Modifier *= ent.Comp.BaseFriction;
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
    [ByRefEvent]
    public record struct RefreshFrictionModifiersEvent() : IInventoryRelayEvent
    {
        public float Friction;
        public float? FrictionNoInput;
        public float Acceleration;

        public void ModifyFriction(float friction, float? noInput = null)
        {
            Friction *= friction;
            // If FrictionNoInput doesn't have a value, give it one, otherwise multiply the current value by the mod.
            FrictionNoInput = FrictionNoInput == null ? noInput : FrictionNoInput * noInput;
        }

        public void ModifyAcceleration(float acceleration)
        {
            Acceleration *= acceleration;
        }
        SlotFlags IInventoryRelayEvent.TargetSlots =>  ~SlotFlags.POCKET;
    }
}

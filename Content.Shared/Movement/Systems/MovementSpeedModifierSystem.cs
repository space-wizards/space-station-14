using System.Text.Json.Serialization.Metadata;
using Content.Shared.CCVar;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Shared.Movement.Systems
{
    public sealed class MovementSpeedModifierSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private   readonly IConfigurationManager _configManager = default!;

        private float _frictionModifier;
        private float _airDamping;
        private float _offGridDamping;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MovementSpeedModifierComponent, MapInitEvent>(OnModMapInit);

            Subs.CVar(_configManager, CCVars.TileFrictionModifier, value => _frictionModifier = value, true);
            Subs.CVar(_configManager, CCVars.AirFriction, value => _airDamping = value, true);
            Subs.CVar(_configManager, CCVars.OffgridFriction, value => _offGridDamping = value, true);
        }

        private void OnModMapInit(Entity<MovementSpeedModifierComponent> ent, ref MapInitEvent args)
        {
            // TODO: Dirty these smarter.
            ent.Comp.WeightlessAcceleration = ent.Comp.BaseWeightlessAcceleration;
            ent.Comp.WeightlessModifier = ent.Comp.BaseWeightlessModifier;
            ent.Comp.WeightlessFriction = _airDamping * ent.Comp.BaseWeightlessFriction;
            ent.Comp.WeightlessFrictionNoInput = _airDamping * ent.Comp.BaseWeightlessFriction;
            ent.Comp.OffGridFriction = _offGridDamping * ent.Comp.BaseWeightlessFriction;
            ent.Comp.Acceleration = ent.Comp.BaseAcceleration;
            ent.Comp.Friction = _frictionModifier * ent.Comp.BaseFriction;
            ent.Comp.FrictionNoInput = _frictionModifier * ent.Comp.BaseFriction;
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
                WeightlessAccelerationMod = 1.0f,
                WeightlessModifier = move.BaseWeightlessModifier,
                WeightlessFriction = move.BaseWeightlessFriction,
                WeightlessFrictionMod = 1.0f,
                WeightlessFrictionNoInput = move.BaseWeightlessFriction,
                WeightlessFrictionNoInputMod = 1.0f,
            };

            RaiseLocalEvent(uid, ref ev);

            if (MathHelper.CloseTo(ev.WeightlessAcceleration, move.WeightlessAcceleration) &&
                MathHelper.CloseTo(ev.WeightlessModifier, move.WeightlessModifier) &&
                MathHelper.CloseTo(ev.WeightlessFriction, move.WeightlessFriction) &&
                MathHelper.CloseTo(ev.WeightlessFrictionNoInput, move.WeightlessFrictionNoInput))
            {
                return;
            }

            move.WeightlessAcceleration = ev.WeightlessAcceleration * ev.WeightlessAccelerationMod;
            move.WeightlessModifier = ev.WeightlessModifier;
            move.WeightlessFriction = _airDamping * ev.WeightlessFriction * ev.WeightlessFrictionMod;
            move.WeightlessFrictionNoInput = _airDamping * ev.WeightlessFrictionNoInput * ev.WeightlessFrictionNoInputMod;
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
                FrictionNoInput = move.BaseFriction,
                Acceleration = move.BaseAcceleration,
            };
            RaiseLocalEvent(uid, ref ev);

            if (MathHelper.CloseTo(ev.Friction, move.Friction)
                && MathHelper.CloseTo(ev.FrictionNoInput, move.FrictionNoInput)
                && MathHelper.CloseTo(ev.Acceleration, move.Acceleration))
                return;

            move.Friction = _frictionModifier * ev.Friction;
            move.FrictionNoInput = _frictionModifier * ev.FrictionNoInput;
            move.Acceleration = ev.Acceleration;

            Dirty(uid, move);
        }

        public void ChangeBaseFriction(EntityUid uid, float friction, float frictionNoInput, float acceleration, MovementSpeedModifierComponent? move = null)
        {
            if (!Resolve(uid, ref move, false))
                return;

            move.BaseFriction = friction;
            move.FrictionNoInput = frictionNoInput;
            move.BaseAcceleration = acceleration;
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
    public record struct RefreshWeightlessModifiersEvent
    {
        public float WeightlessAcceleration;
        public float WeightlessAccelerationMod;

        public float WeightlessModifier;

        public float WeightlessFriction;
        public float WeightlessFrictionMod;

        public float WeightlessFrictionNoInput;
        public float WeightlessFrictionNoInputMod;

        public void ModifyFriction(float friction, float noInput)
        {
            WeightlessFrictionMod *= friction;
            WeightlessFrictionNoInput *= noInput;
        }

        public void ModifyFriction(float friction)
        {
            ModifyFriction(friction, friction);
        }

        public void ModifyAcceleration(float acceleration, float modifier)
        {
            WeightlessAcceleration *= acceleration;
            WeightlessModifier *= modifier;
        }

        public void ModifyAcceleration(float modifier)
        {
            ModifyAcceleration(modifier, modifier);
        }
    }
    [ByRefEvent]
    public record struct RefreshFrictionModifiersEvent : IInventoryRelayEvent
    {
        public float Friction;
        public float FrictionNoInput;
        public float Acceleration;

        public void ModifyFriction(float friction, float noInput)
        {
            Friction *= friction;
            FrictionNoInput *= noInput;
        }

        public void ModifyFriction(float friction)
        {
            ModifyFriction(friction, friction);
        }

        public void ModifyAcceleration(float acceleration)
        {
            Acceleration *= acceleration;
        }
        SlotFlags IInventoryRelayEvent.TargetSlots =>  ~SlotFlags.POCKET;
    }
}

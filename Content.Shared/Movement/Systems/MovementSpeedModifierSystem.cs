using Content.Shared.CCVar;
using Content.Shared.Gravity;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Content.Shared.Standing;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Shared.Movement.Systems
{
    public sealed class MovementSpeedModifierSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly SharedGravitySystem _gravity = default!;

        private float _frictionModifier;
        private float _airDamping;
        private float _offGridDamping;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MovementSpeedModifierComponent, MapInitEvent>(OnModMapInit);
            SubscribeLocalEvent<MovementSpeedModifierComponent, DownedEvent>(OnDowned);
            SubscribeLocalEvent<MovementSpeedModifierComponent, StoodEvent>(OnStand);

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

        private void OnDowned(Entity<MovementSpeedModifierComponent> entity, ref DownedEvent args)
        {
            RefreshFrictionModifiers((entity, entity.Comp));
        }

        private void OnStand(Entity<MovementSpeedModifierComponent> entity, ref StoodEvent args)
        {
            RefreshFrictionModifiers((entity, entity.Comp));
        }

        /// <summary>
        /// Copy this component's datafields from one entity to another.
        /// This needs to refresh the modifiers after using CopyComp.
        /// </summary>
        public void CopyComponent(Entity<MovementSpeedModifierComponent?> source, EntityUid target)
        {
            if (!Resolve(source, ref source.Comp))
                return;

            CopyComp(source, target, source.Comp);
            RefreshWeightlessModifiers(target);
            RefreshMovementSpeedModifiers(target);
            RefreshFrictionModifiers(target);
        }

        /// <summary>
        /// This API method refreshes the movement modifiers for either being weightless, or being grounded depending
        /// on which modifiers the entity is currently using.
        /// </summary>
        /// <param name="ent">The entity we're refreshing modifiers for</param>
        public void RefreshMovementModifiers(Entity<MovementSpeedModifierComponent?> ent)
        {
            if (_gravity.IsWeightless(ent.Owner))
                RefreshWeightlessModifiers(ent);
            else
                RefreshMovementSpeedModifiers(ent);
        }

        /// <summary>
        /// This method refreshes the weightless movement modifiers for an entity.
        /// </summary>
        /// <param name="ent">The entity we're refreshing modifiers for.</param>
        public void RefreshWeightlessModifiers(Entity<MovementSpeedModifierComponent?> ent)
        {
            if (!Resolve(ent, ref ent.Comp, false))
                return;

            if (_timing.ApplyingState)
                return;

            var ev = new RefreshWeightlessModifiersEvent()
            {
                WeightlessAcceleration = ent.Comp.BaseWeightlessAcceleration,
                WeightlessAccelerationMod = 1.0f,
                WeightlessModifier = ent.Comp.BaseWeightlessModifier,
                WeightlessModifierMod = 1.0f,
                WeightlessFriction = ent.Comp.BaseWeightlessFriction,
                WeightlessFrictionMod = 1.0f,
                WeightlessFrictionNoInput = ent.Comp.BaseWeightlessFriction,
                WeightlessFrictionNoInputMod = 1.0f,
            };

            RaiseLocalEvent(ent, ref ev);

            if (MathHelper.CloseTo(ev.WeightlessAcceleration, ent.Comp.WeightlessAcceleration) &&
                MathHelper.CloseTo(ev.WeightlessModifier, ent.Comp.WeightlessModifier) &&
                MathHelper.CloseTo(ev.WeightlessFriction, ent.Comp.WeightlessFriction) &&
                MathHelper.CloseTo(ev.WeightlessFrictionNoInput, ent.Comp.WeightlessFrictionNoInput))
            {
                return;
            }

            ent.Comp.WeightlessAcceleration = ev.WeightlessAcceleration * ev.WeightlessAccelerationMod;
            ent.Comp.WeightlessModifier = ev.WeightlessModifier * ev.WeightlessModifierMod;
            ent.Comp.WeightlessFriction = _airDamping * ev.WeightlessFriction * ev.WeightlessFrictionMod;
            ent.Comp.WeightlessFrictionNoInput = _airDamping * ev.WeightlessFrictionNoInput * ev.WeightlessFrictionNoInputMod;
            Dirty(ent);
        }

        /// <summary>
        /// Refreshes the grounded speed modifiers for an entity.
        /// </summary>
        /// <param name="ent">The entity we're refreshing modifiers for</param>
        public void RefreshMovementSpeedModifiers(Entity<MovementSpeedModifierComponent?> ent)
        {
            if (!Resolve(ent, ref ent.Comp, false))
                return;

            if (_timing.ApplyingState)
                return;

            var ev = new RefreshMovementSpeedModifiersEvent();
            RaiseLocalEvent(ent, ev);

            if (MathHelper.CloseTo(ev.WalkSpeedModifier, ent.Comp.WalkSpeedModifier) &&
                MathHelper.CloseTo(ev.SprintSpeedModifier, ent.Comp.SprintSpeedModifier))
                return;

            ent.Comp.WalkSpeedModifier = ev.WalkSpeedModifier;
            ent.Comp.SprintSpeedModifier = ev.SprintSpeedModifier;
            Dirty(ent);
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

        /// <summary>
        /// Refreshes the grounded friction and acceleration modifiers for an entity.
        /// </summary>
        /// <param name="ent">The entity we're refreshing modifiers for</param>
        public void RefreshFrictionModifiers(Entity<MovementSpeedModifierComponent?> ent)
        {
            if (!Resolve(ent, ref ent.Comp, false))
                return;

            if (_timing.ApplyingState)
                return;

            var ev = new RefreshFrictionModifiersEvent()
            {
                Friction = ent.Comp.BaseFriction,
                FrictionNoInput = ent.Comp.BaseFriction,
                Acceleration = ent.Comp.BaseAcceleration,
            };
            RaiseLocalEvent(ent, ref ev);

            if (MathHelper.CloseTo(ev.Friction, ent.Comp.Friction)
                && MathHelper.CloseTo(ev.FrictionNoInput, ent.Comp.FrictionNoInput)
                && MathHelper.CloseTo(ev.Acceleration, ent.Comp.Acceleration))
                return;

            ent.Comp.Friction = _frictionModifier * ev.Friction;
            ent.Comp.FrictionNoInput = _frictionModifier * ev.FrictionNoInput;
            ent.Comp.Acceleration = ev.Acceleration;

            Dirty(ent);
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
        public float WeightlessModifierMod;

        public float WeightlessFriction;
        public float WeightlessFrictionMod;

        public float WeightlessFrictionNoInput;
        public float WeightlessFrictionNoInputMod;

        public void ModifyFriction(float friction, float noInput)
        {
            WeightlessFrictionMod *= friction;
            WeightlessFrictionNoInputMod *= noInput;
        }

        public void ModifyFriction(float friction)
        {
            ModifyFriction(friction, friction);
        }

        public void ModifyAcceleration(float acceleration, float modifier)
        {
            WeightlessAccelerationMod *= acceleration;
            WeightlessModifierMod *= modifier;
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

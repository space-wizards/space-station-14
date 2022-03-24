using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement.EntitySystems
{
    public sealed class MovementSpeedModifierSystem : EntitySystem
    {
        private readonly HashSet<EntityUid> _needsRefresh = new();

        public override void Update(float frameTime)
        {
            foreach (var uid in _needsRefresh)
            {
                RecalculateMovementSpeedModifiers(uid);
            }

            _needsRefresh.Clear();
        }

        public override void Initialize()
        {
            base.Initialize();
            UpdatesOutsidePrediction = true;
            SubscribeLocalEvent<MovementSpeedModifierComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<MovementSpeedModifierComponent, ComponentHandleState>(OnHandleState);
        }

        private void OnGetState(EntityUid uid, MovementSpeedModifierComponent component, ref ComponentGetState args)
        {
            args.State = new MovementSpeedModifierComponentState
            {
                BaseWalkSpeed = component.BaseWalkSpeed,
                BaseSprintSpeed = component.BaseSprintSpeed,
                WalkSpeedModifier = component.WalkSpeedModifier,
                SprintSpeedModifier = component.SprintSpeedModifier
            };
        }

        private void OnHandleState(EntityUid uid, MovementSpeedModifierComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not MovementSpeedModifierComponentState state) return;
            component.BaseWalkSpeed = state.BaseWalkSpeed;
            component.BaseSprintSpeed = state.BaseSprintSpeed;
            component.WalkSpeedModifier = state.WalkSpeedModifier;
            component.SprintSpeedModifier = state.SprintSpeedModifier;
        }

        public void RefreshMovementSpeedModifiers(EntityUid uid)
        {
            _needsRefresh.Add(uid);
        }

        private void RecalculateMovementSpeedModifiers(EntityUid uid, MovementSpeedModifierComponent? move = null)
        {
            if (!Resolve(uid, ref move, false))
                return;

            var ev = new RefreshMovementSpeedModifiersEvent();
            RaiseLocalEvent(uid, ev, false);

            move.WalkSpeedModifier = ev.WalkSpeedModifier;
            move.SprintSpeedModifier = ev.SprintSpeedModifier;

            move.Dirty();
        }

        [Serializable, NetSerializable]
        private sealed class MovementSpeedModifierComponentState : ComponentState
        {
            public float BaseWalkSpeed;
            public float BaseSprintSpeed;
            public float WalkSpeedModifier;
            public float SprintSpeedModifier;
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
    }
}

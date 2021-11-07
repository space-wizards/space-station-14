using System;
using System.Collections.Generic;
using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement.EntitySystems
{
    public sealed class MovementSpeedModifierSystem : EntitySystem
    {
        public HashSet<EntityUid> NeedsRefresh = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MovementSpeedModifierComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<MovementSpeedModifierComponent, ComponentHandleState>(OnHandleState);
        }

        private void OnGetState(EntityUid uid, MovementSpeedModifierComponent component, ref ComponentGetState args)
        {
            args.State = new MovementSpeedModifierComponentState
            {
                BaseWalkSpeed = component.BaseWalkSpeed,
                BaseSprintSpeed = component.BaseSprintSpeed,
            };
        }

        private void OnHandleState(EntityUid uid, MovementSpeedModifierComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not MovementSpeedModifierComponentState state) return;
            component.BaseWalkSpeed = state.BaseWalkSpeed;
            component.BaseSprintSpeed = state.BaseSprintSpeed;
        }

        public void RefreshMovementSpeedModifiers(EntityUid uid)
        {
            NeedsRefresh.Add(uid);
        }

        private void RecalculateMovementSpeedModifiers(EntityUid uid, MovementSpeedModifierComponent? move = null)
        {
            if (!Resolve(uid, ref move))
                return;

            var ev = new RefreshMovementSpeedModifiersEvent();
            RaiseLocalEvent(uid, ev, false);

            move.WalkSpeedModifier = ev.WalkSpeedModifier;
            move.SprintSpeedModifier = ev.SprintSpeedModifier;
        }

        [Serializable, NetSerializable]
        private sealed class MovementSpeedModifierComponentState : ComponentState
        {
            public float BaseWalkSpeed;
            public float BaseSprintSpeed;
        }
    }

    /// <summary>
    ///     Raised on an entity to determine it's new movement speed. Any system that wishes to change movement speed
    ///     should hook into this event and set it then. If you want this event to be raised,
    ///     call <see cref="MovementSpeedModifierSystem.RefreshMovementSpeedModifiers"/>.
    /// </summary>
    public class RefreshMovementSpeedModifiersEvent : EntityEventArgs
    {
        public float WalkSpeedModifier { get; private set; } = 1.0f;
        public float SprintSpeedModifier { get; private set; } = 1.0f;

        public void ModifySpeed(float walk, float sprint)
        {
            WalkSpeedModifier *= walk;
            SprintSpeedModifier *= sprint;
        }
    }
}

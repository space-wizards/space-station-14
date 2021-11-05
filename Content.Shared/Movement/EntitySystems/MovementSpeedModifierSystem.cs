using System;
using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement.EntitySystems
{
    public sealed class MovementSpeedModifierSystem : EntitySystem
    {
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

        [Serializable, NetSerializable]
        private sealed class MovementSpeedModifierComponentState : ComponentState
        {
            public float BaseWalkSpeed;
            public float BaseSprintSpeed;
        }
    }
}

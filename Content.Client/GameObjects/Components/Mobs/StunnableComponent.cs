#nullable enable
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Movement;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Client.GameObjects.Components.Mobs
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStunnableComponent))]
    public class StunnableComponent : SharedStunnableComponent
    {
        private bool _stunned;
        private bool _knockedDown;
        private bool _slowedDown;

        public override bool Stunned => _stunned;
        public override bool KnockedDown => _knockedDown;
        public override bool SlowedDown => _slowedDown;

        protected override void OnInteractHand()
        {
            EntitySystem.Get<AudioSystem>()
                .Play("/Audio/Effects/thudswoosh.ogg", Owner, AudioHelpers.WithVariation(0.25f));
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (!(curState is StunnableComponentState state))
            {
                return;
            }

            _stunned = state.Stunned;
            _knockedDown = state.KnockedDown;
            _slowedDown = state.SlowedDown;

            WalkModifierOverride = state.WalkModifierOverride;
            RunModifierOverride = state.RunModifierOverride;

            if (Owner.TryGetComponent(out MovementSpeedModifierComponent movement))
            {
                movement.RefreshMovementSpeedModifiers();
            }
        }
    }
}

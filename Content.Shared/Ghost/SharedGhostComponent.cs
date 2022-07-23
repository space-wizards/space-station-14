using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Ghost
{
    [NetworkedComponent()]
    public abstract class SharedGhostComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanGhostInteract
        {
            get => _canGhostInteract;
            set
            {
                if (_canGhostInteract == value) return;
                _canGhostInteract = value;
                Dirty();
            }
        }

        [DataField("canInteract")]
        private bool _canGhostInteract;

        /// <summary>
        ///     Changed by <see cref="SharedGhostSystem.SetCanReturnToBody"/>
        /// </summary>
        // TODO MIRROR change this to use friend classes when thats merged
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanReturnToBody
        {
            get => _canReturnToBody;
            set
            {
                if (_canReturnToBody == value) return;
                _canReturnToBody = value;
                Dirty();
            }
        }

        [DataField("canReturnToBody")]
        private bool _canReturnToBody;

        public override ComponentState GetComponentState()
        {
            return new GhostComponentState(CanReturnToBody, CanGhostInteract);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not GhostComponentState state)
            {
                return;
            }

            CanReturnToBody = state.CanReturnToBody;
            CanGhostInteract = state.CanGhostInteract;
        }
    }

    [Serializable, NetSerializable]
    public sealed class GhostComponentState : ComponentState
    {
        public bool CanReturnToBody { get; }
        public bool CanGhostInteract { get; }

        public GhostComponentState(
            bool canReturnToBody,
            bool canGhostInteract)
        {
            CanReturnToBody = canReturnToBody;
            CanGhostInteract = canGhostInteract;
        }
    }
}



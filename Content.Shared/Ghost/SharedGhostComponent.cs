using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Ghost
{
    [NetworkedComponent()]
    public abstract class SharedGhostComponent : Component
    {
        public TimeSpan TimeOfDeath { get; set; } = TimeSpan.Zero;

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
            return new GhostComponentState(CanReturnToBody, CanGhostInteract, TimeOfDeath);
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
            TimeOfDeath = state.TimeOfDeath;
        }
    }

    [Serializable, NetSerializable]
    public sealed class GhostComponentState : ComponentState
    {
        public bool CanReturnToBody { get; }
        public bool CanGhostInteract { get; }

        public TimeSpan TimeOfDeath { get; }

        public GhostComponentState(
            bool canReturnToBody,
            bool canGhostInteract,
            TimeSpan timeOfDeath)
        {
            CanReturnToBody = canReturnToBody;
            CanGhostInteract = canGhostInteract;
            TimeOfDeath = timeOfDeath;
        }
    }
}



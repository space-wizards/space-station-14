using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Players;
using Robust.Shared.Serialization;

namespace Content.Shared.Hands.Components
{
    [RegisterComponent]
    [NetworkedComponent]
    public sealed class HandVirtualPullComponent : Component
    {
        private EntityUid _pulledEntity;
        public override string Name => "HandVirtualPull";

        public EntityUid PulledEntity
        {
            get => _pulledEntity;
            set
            {
                _pulledEntity = value;
                Dirty();
            }
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new VirtualPullComponentState(_pulledEntity);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not VirtualPullComponentState pullState)
                return;

            _pulledEntity = pullState.PulledEntity;
        }

        [Serializable, NetSerializable]
        public sealed class VirtualPullComponentState : ComponentState
        {
            public readonly EntityUid PulledEntity;

            public VirtualPullComponentState(EntityUid pulledEntity)
            {
                PulledEntity = pulledEntity;
            }
        }
    }
}

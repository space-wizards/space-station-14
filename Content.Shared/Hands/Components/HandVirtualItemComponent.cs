using System;
using Content.Shared.Item;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Players;
using Robust.Shared.Serialization;

namespace Content.Shared.Hands.Components
{
    [RegisterComponent]
    [NetworkedComponent]
    public sealed class HandVirtualItemComponent : Component
    {
        private EntityUid _blockingEntity;
        public override string Name => "HandVirtualItem";

        /// <summary>
        ///     The entity blocking this hand.
        /// </summary>
        public EntityUid BlockingEntity
        {
            get => _blockingEntity;
            set
            {
                _blockingEntity = value;
                Dirty();
            }
        }

        public override ComponentState GetComponentState()
        {
            return new VirtualItemComponentState(BlockingEntity);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not VirtualItemComponentState pullState)
                return;

            _blockingEntity = pullState.BlockingEntity;

            // update hands GUI with new entity.
            if (Owner.TryGetContainer(out var containter))
                EntitySystem.Get<SharedItemSystem>().VisualsChanged(Owner);
        }

        [Serializable, NetSerializable]
        public sealed class VirtualItemComponentState : ComponentState
        {
            public readonly EntityUid BlockingEntity;

            public VirtualItemComponentState(EntityUid blockingEntity)
            {
                BlockingEntity = blockingEntity;
            }
        }
    }
}

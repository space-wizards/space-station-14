using System.Linq;
using Content.Client.Animations;
using Content.Shared.DragDrop;
using Content.Shared.Storage;

namespace Content.Client.Storage
{
    /// <summary>
    /// Client version of item storage containers, contains a UI which displays stored entities and their size
    /// </summary>
    [RegisterComponent]
    public sealed class ClientStorageComponent : SharedStorageComponent, IDraggable
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private List<EntityUid> _storedEntities = new();
        private int StorageSizeUsed;
        private int StorageCapacityMax;

        public override IReadOnlyList<EntityUid> StoredEntities => _storedEntities;

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not StorageComponentState state)
            {
                return;
            }

            _storedEntities = state.StoredEntities.ToList();
        }

        /// <summary>
        /// Animate the newly stored entities in <paramref name="msg"/> flying towards this storage's position
        /// </summary>
        /// <param name="msg"></param>
        public void HandleAnimatingInsertingEntities(AnimateInsertingEntitiesEvent msg)
        {
            for (var i = 0; msg.StoredEntities.Count > i; i++)
            {
                var entity = msg.StoredEntities[i];
                var initialPosition = msg.EntityPositions[i];

                if (_entityManager.EntityExists(entity))
                {
                    ReusableAnimations.AnimateEntityPickup(entity, initialPosition, _entityManager.GetComponent<TransformComponent>(Owner).LocalPosition, _entityManager);
                }
            }
        }

        public override bool Remove(EntityUid entity)
        {
            if (_storedEntities.Remove(entity))
            {
                Dirty();
                return true;
            }

            return false;
        }
    }
}

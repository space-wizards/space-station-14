using Content.Client.Animations;
using Content.Shared.DragDrop;
using Content.Shared.Storage;

namespace Content.Client.Storage
{
    /// <summary>
    /// Client version of item storage containers, contains a UI which displays stored entities and their size
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(SharedStorageComponent))]
    public sealed class ClientStorageComponent : SharedStorageComponent
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        private List<EntityUid> _storedEntities = new();
        public override IReadOnlyList<EntityUid> StoredEntities => _storedEntities;

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
            return false;
        }
    }
}

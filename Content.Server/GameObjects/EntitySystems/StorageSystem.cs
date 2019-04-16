using System.Collections.Generic;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    class StorageSystem : EntitySystem
    {
        private readonly List<IPlayerSession> _sessionCache = new List<IPlayerSession>();

        /// <inheritdoc />
        public override void Initialize()
        {
            EntityQuery = new TypeEntityQuery(typeof(ServerStorageComponent));
        }

        /// <inheritdoc />
        public override void SubscribeEvents()
        {
            base.SubscribeEvents();

            SubscribeEvent<EntParentChangedMessage>(HandleParentChanged);
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                CheckSubscribedEntities(entity);
            }
        }

        private static void HandleParentChanged(object sender, EntitySystemMessage message)
        {
            if(!(sender is IEntity childEntity))
                return;

            if(!(message is EntParentChangedMessage msg))
                return;

            var oldParentEntity = msg.OldParent;

            if(oldParentEntity == null || !oldParentEntity.IsValid())
                return;

            if (oldParentEntity.TryGetComponent(out ServerStorageComponent storageComp))
            {
                storageComp.Remove(childEntity);
            }
        }

        private void CheckSubscribedEntities(IEntity entity)
        {
            var storageComp = entity.GetComponent<ServerStorageComponent>();

            // We have to cache the set of sessions because Unsubscribe modifies the original.
            _sessionCache.Clear();
            _sessionCache.AddRange(storageComp.SubscribedSessions);

            if (_sessionCache.Count == 0)
                return;

            var storagePos = entity.Transform.WorldPosition;
            var storageMap = entity.Transform.MapID;

            foreach (var session in _sessionCache)
            {
                var attachedEntity = session.AttachedEntity;

                // The component manages the set of sessions, so this invalid session should be removed soon.
                if (attachedEntity == null || !attachedEntity.IsValid())
                    continue;

                if (storageMap != attachedEntity.Transform.MapID)
                    continue;

                var distanceSquared = (storagePos - attachedEntity.Transform.WorldPosition).LengthSquared;
                if (distanceSquared > InteractionSystem.INTERACTION_RANGE_SQUARED)
                {
                    storageComp.UnsubscribeSession(session);
                }
            }
        }
    }
}

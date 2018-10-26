using System.Collections.Generic;
using SS14.Server.Interfaces.Player;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Systems;

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
        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                var storageComp = entity.GetComponent<ServerStorageComponent>();

                // We have to cache the set of sessions because Unsubscribe modifies the original.
                _sessionCache.Clear();
                _sessionCache.AddRange(storageComp.SubscribedSessions);

                if (_sessionCache.Count == 0)
                    continue;

                var storagePos = entity.Transform.WorldPosition;
                var storageMap = entity.Transform.MapID;

                foreach (var session in _sessionCache)
                {
                    var attachedEntity = session.AttachedEntity;

                    // The component manages the set of sessions, so this invalid session should be removed soon.
                    if (attachedEntity == null || !attachedEntity.IsValid())
                        continue;

                    if(storageMap != attachedEntity.Transform.MapID)
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
}

using System.Collections.Generic;
using Content.Server.GameObjects.Components.Conveyor;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class ConveyorSystem : EntitySystem
    {
        private uint _nextId;
        private Dictionary<uint, HashSet<ConveyorComponent>> _connections;

        public uint NextId()
        {
            return ++_nextId;
        }

        public void AddConnections(uint id, params ConveyorComponent[] connections)
        {
            if (!_connections.TryGetValue(id, out var set))
            {
                set = new HashSet<ConveyorComponent>();
                _connections.Add(id, set);
            }

            foreach (var conveyor in connections)
            {
                conveyor.Id = id;
                set.Add(conveyor);
            }
        }

        public IReadOnlyCollection<ConveyorComponent> GetOrCreateConnections(uint id)
        {
            if (!_connections.TryGetValue(id, out var set))
            {
                set = new HashSet<ConveyorComponent>();
                _connections.Add(id, set);
            }

            return set;
        }

        public void RemoveConnection(ConveyorComponent conveyor)
        {
            if (!conveyor.Id.HasValue ||
                !_connections.TryGetValue(conveyor.Id.Value, out var set))
            {
                return;
            }

            set.Remove(conveyor);
        }

        public override void Initialize()
        {
            base.Initialize();

            EntityQuery = new TypeEntityQuery(typeof(ConveyorComponent));
            _connections = new Dictionary<uint, HashSet<ConveyorComponent>>();
        }

        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                if (!entity.TryGetComponent(out ConveyorComponent conveyor))
                {
                    continue;
                }

                conveyor.Update(frameTime);
            }
        }
    }
}

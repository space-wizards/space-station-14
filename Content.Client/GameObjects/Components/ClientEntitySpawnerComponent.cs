#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System.Collections.Generic;

namespace Content.Client.GameObjects.Components
{
    /// <summary>
    ///     Spawns a set of entities on the client only, and removes them when this component is removed.
    /// </summary>
    [RegisterComponent]
    public class ClientEntitySpawnerComponent : Component
    {
        public override string Name => "ClientEntitySpawner";

        private List<string> _prototypes = default!;

        private List<IEntity> _entity = new();

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _prototypes, "prototypes", new List<string> { "HVDummyWire" });
        }

        public override void Initialize()
        {
            base.Initialize();
            SpawnEntities();
        }

        public override void OnRemove()
        {
            RemoveEntities();
            base.OnRemove();
        }

        private void SpawnEntities()
        {
            foreach (var proto in _prototypes)
            {
                var entity = Owner.EntityManager.SpawnEntity(proto, Owner.Transform.Coordinates);
                _entity.Add(entity);
            }
        }

        private void RemoveEntities()
        {
            foreach (var entity in _entity)
            {
                Owner.EntityManager.DeleteEntity(entity);
            }
        }
    }
}

using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Spawners
{
    /// <summary>
    ///     Spawns a set of entities on the client only, and removes them when this component is removed.
    /// </summary>
    [RegisterComponent]
    public class ClientEntitySpawnerComponent : Component
    {
        public override string Name => "ClientEntitySpawner";

        [DataField("prototypes")] private List<string> _prototypes =  new() { "HVDummyWire" };

        private readonly List<IEntity> _entity = new();

        protected override void Initialize()
        {
            base.Initialize();
            SpawnEntities();
        }

        protected override void OnRemove()
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

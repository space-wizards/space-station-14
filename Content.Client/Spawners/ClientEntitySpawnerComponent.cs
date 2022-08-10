using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Spawners
{
    /// <summary>
    ///     Spawns a set of entities on the client only, and removes them when this component is removed.
    /// </summary>
    [RegisterComponent]
    [ComponentProtoName("ClientEntitySpawner")]
    public sealed class ClientEntitySpawnerComponent : Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        [DataField("prototypes")] private List<string> _prototypes =  new() { "HVDummyWire" };

        private readonly List<EntityUid> _entity = new();

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
                var entity = _entMan.SpawnEntity(proto, _entMan.GetComponent<TransformComponent>(Owner).Coordinates);
                _entity.Add(entity);
            }
        }

        private void RemoveEntities()
        {
            foreach (var entity in _entity)
            {
                _entMan.DeleteEntity(entity);
            }
        }
    }
}

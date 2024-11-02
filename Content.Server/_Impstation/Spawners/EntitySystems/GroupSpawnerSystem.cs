using Content.Server.GameTicking;
using Content.Server._Impstation.Spawners.Components;
using Content.Shared.Storage;

namespace Content.Server.Spawners.EntitySystems
{
    public sealed class GroupSpawnerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GroupSpawnerComponent, ComponentInit>(OnGroupSpawnMapInit);
        }

        private void OnGroupSpawnMapInit(EntityUid uid, GroupSpawnerComponent component, ComponentInit args)
        {
            TrySpawn(uid, component);
        }

        private void TrySpawn(EntityUid uid, GroupSpawnerComponent component)
        {

            var coordinates = Transform(uid).Coordinates;

            if (component.Spawns is not {} spawns)
                return;

            foreach (var spawn in spawns)
            {
                if (spawn == null)
                {
                    continue;
                }
                var amount = EntitySpawnCollection.GetAmount(spawn.Value);
                var entity = spawn.Value.PrototypeId;
                for (var i = 0; i < amount; i++) {
                    SpawnAtPosition(entity, coordinates);
                }
            }
        }

    }
}

using Content.Server.GameTicking;
using Robust.Shared.Random;
using System.Numerics;
using Content.Server._Impstation.Spawners.Components;
using Content.Shared.Storage;
using Robust.Server.GameObjects;

namespace Content.Server.Spawners.EntitySystems
{
    public sealed class SpawnerEntrySpawnerSystem : EntitySystem
    {

        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly TransformSystem _transform = default!;


        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SpawnEntrySpawnerComponent, ComponentInit>(OnSpawnEntrySpawnMapInit);
        }

        private void OnSpawnEntrySpawnMapInit(EntityUid uid, SpawnEntrySpawnerComponent component, ComponentInit args)
        {
            TrySpawn(uid, component);
        }

        private void TrySpawn(EntityUid uid, SpawnEntrySpawnerComponent component)
        {
            if (component.Spawns is not {} spawns)
                return;

            var coord = _transform.GetMapCoordinates(uid);
            foreach (var spawn in EntitySpawnCollection.GetSpawns(spawns, _random))
            {
                var dx = _random.NextFloat(-component.Range, component.Range);
                var dy = _random.NextFloat(-component.Range, component.Range);
                var spawnCord = coord.Offset(new Vector2(dx, dy));
                var ent = Spawn(spawn, spawnCord);
                _transform.AttachToGridOrMap(ent);
            }
        }
    }
}

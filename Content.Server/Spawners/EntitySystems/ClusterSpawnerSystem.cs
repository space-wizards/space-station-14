using Content.Server.GameTicking;
using Content.Server.Spawners.Components;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Random;

namespace Content.Server.Spawners.EntitySystems
{
    public sealed class ClusterSpawnerSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ClusterSpawnerComponent, ComponentInit>(OnClusterSpawnMapInit);
        }

        private void OnClusterSpawnMapInit(EntityUid uid, ClusterSpawnerComponent component, ComponentInit args)
        {
            TrySpawn(uid, component);
        }

        private void TrySpawn(EntityUid uid, ClusterSpawnerComponent component){
            if (!_robustRandom.Prob(component.Chance) || component.Prototypes.Count == 0)
                return;

            var number = _robustRandom.Next(component.MinimumEntitiesSpawned, component.MaximumEntitiesSpawned);
            var coordinates = Transform(uid).Coordinates;

            for (var i = 0; i < number; i++)
	        {
	            var entity = _robustRandom.Pick(component.Prototypes);
	            SpawnAtPosition(entity, coordinates);
	        }
        }

    }
}

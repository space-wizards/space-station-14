using Content.Server.GameTicking;
using Content.Server._Impstation.Spawners.Components;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Prototypes;

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

        private void TrySpawn(EntityUid uid, GroupSpawnerComponent component){

            var coordinates = Transform(uid).Coordinates;

            foreach (EntProtoId entity in component.Prototypes)
                {
                    SpawnAtPosition(entity, coordinates);
                }
        }

    }
}

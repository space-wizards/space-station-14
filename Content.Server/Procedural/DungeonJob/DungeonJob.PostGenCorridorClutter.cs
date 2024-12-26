using System.Threading.Tasks;
using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Content.Shared.Storage;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="CorridorClutterDunGen"/>
    /// </summary>
    private async Task PostGen(CorridorClutterDunGen gen, DungeonData data, Dungeon dungeon, HashSet<Vector2i> reservedTiles, Random random)
    {
        var physicsQuery = _entManager.GetEntityQuery<PhysicsComponent>();
        var count = (int) Math.Ceiling(dungeon.CorridorTiles.Count * gen.Chance);

        while (count > 0)
        {
            var tile = random.Pick(dungeon.CorridorTiles);

            var enumerator = _maps.GetAnchoredEntitiesEnumerator(_gridUid, _grid, tile);
            var blocked = false;

            while (enumerator.MoveNext(out var ent))
            {
                if (!physicsQuery.TryGetComponent(ent, out var physics) ||
                    !physics.CanCollide ||
                    !physics.Hard)
                {
                    continue;
                }

                blocked = true;
                break;
            }

            if (blocked)
                continue;

            count--;

            var protos = EntitySpawnCollection.GetSpawns(gen.Contents, random);
            var coords = _maps.ToCenterCoordinates(_gridUid, tile, _grid);
            _entManager.SpawnEntities(coords, protos);
            await SuspendIfOutOfTime();

            if (!ValidateResume())
                return;
        }
    }
}

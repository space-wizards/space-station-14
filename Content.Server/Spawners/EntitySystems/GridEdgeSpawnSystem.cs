using Content.Server.Atmos.EntitySystems;
using Content.Server.Spawners.Components;
using Content.Shared.Atmos;
using Robust.Shared.Prototypes;

namespace Content.Server.Spawners.EntitySystems;

/// <summary>
/// Spawns the prototype if the immediate tile in any cardinal direction is considered space.
/// </summary>
public sealed class GridEdgeSpawnSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GridEdgeSpawnComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<GridEdgeSpawnComponent> ent, ref MapInitEvent args)
    {
        TrySpawn(ent);
    }

    /// <summary>
    /// Checks all cardinal directions for a space tile and spawns the entity if it is present.
    /// </summary>
    /// <returns>A true or false depending on if an entity was spawned.</returns>
    public bool TrySpawn(Entity<GridEdgeSpawnComponent> ent)
    {
        var xform = Transform(ent);

        if (!_transform.TryGetGridTilePosition((ent.Owner, xform), out var indices))
            return false;

        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var direction = (AtmosDirection)(1 << i);
            var offsetTile = indices.Offset(direction);

            if (_atmosphere.IsTileNoGrid(xform.GridUid, offsetTile))
            {
                Spawn(ent.Comp.Prototype, xform.Coordinates);

                if (ent.Comp.DeleteSpawner)
                {
                    QueueDel(ent);
                }

                return true;
            }
        }

        if (ent.Comp.DeleteSpawner)
        {
            QueueDel(ent);
        }

        return false;
    }

    /// <summary>
    /// Sets the prototype on this spawner to the one given.
    /// </summary>
    /// <param name="entity">The spawner entity targeted.</param>
    /// <param name="prototype">The prototype to set.</param>
    public void SetPrototype(Entity<GridEdgeSpawnComponent> entity, EntProtoId prototype)
    {
        entity.Comp.Prototype = prototype;
    }
}

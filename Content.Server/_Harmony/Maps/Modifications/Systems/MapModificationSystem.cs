using Content.Server.GameTicking;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._Harmony.EntitySelector;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Harmony.Maps.Modifications.Systems;

public sealed class MapModificationSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PostGameMapLoad>(OnPostGameMapLoad, after: [ typeof(StationSystem) ]);
    }

    private void OnPostGameMapLoad(PostGameMapLoad args)
    {
        foreach (var mapModification in _prototypeManager.EnumeratePrototypes<MapModificationPrototype>())
        {
            if (!mapModification.ApplyOn.Contains(args.GameMap.ID))
                continue;

            Log.Debug("Applying map modification {0} to map {1}", mapModification.ID, args.GameMap.ID);

            var station = _stationSystem.GetStationInMap(args.Map);
            if (station == null)
            {
                DebugTools.Assert($"Failed to find a station on map {args.GameMap.ID} but it had a map modification assigned!");
                Log.Error("Tried to apply map modification {0} to map {1} but failed to find a station!", mapModification.ID, args.Map);
                break;
            }

            var grid = _stationSystem.GetLargestGrid(Comp<StationDataComponent>(station.Value));
            if (grid == null)
            {
                DebugTools.Assert($"Station on map {args.GameMap.ID} has no grids.");
                Log.Error("Station has no grids???");
                break;
            }

            ApplyMapModification(mapModification, grid.Value);
        }
    }

    /// <summary>
    /// Apply a map modification to a map
    /// </summary>
    public void ApplyMapModification(MapModificationPrototype mapModification, EntityUid grid)
    {
        var entitiesToAdd = new List<MapModificationEntity>();
        entitiesToAdd.AddRange(mapModification.Additions);

        // Iterate over all entities inside the grid
        var gridEntities = Transform(grid).ChildEnumerator;
        while (gridEntities.MoveNext(out var entity))
        {
            // Apply removals
            if (EntitySelectorManager.EntityMatchesAny(entity, mapModification.Removals))
            {
                Del(entity);
                continue;
            }

            // Apply replacements
            foreach (var replacement in mapModification.Replacements)
            {
                if (!EntitySelectorManager.EntityMatchesAny(entity, replacement.From))
                    continue;

                var entityTransform = Transform(entity);
                var newEntity = new MapModificationEntity
                {
                    Prototype = replacement.NewPrototype,
                    Name = replacement.NewName,
                    Description = replacement.NewDescription,
                    Position = entityTransform.LocalPosition,
                    Rotation = replacement.NewRotation ?? entityTransform.LocalRotation,
                    Components = replacement.NewComponents,
                };

                Del(entity);
                entitiesToAdd.Add(newEntity);
            }
        }

        // Apply additions
        foreach (var addition in entitiesToAdd)
        {
            ApplyMapModificationEntity(addition, grid);
        }
    }

    private void ApplyMapModificationEntity(MapModificationEntity newEntity, EntityUid grid)
    {
        var entity = _entityManager.CreateEntityUninitialized(newEntity.Prototype,
            new EntityCoordinates(grid, newEntity.Position),
            newEntity.Components,
            newEntity.Rotation ?? default);

        _entityManager.InitializeAndStartEntity(entity, false);

        if (newEntity.Name != null)
            _metaDataSystem.SetEntityName(entity, newEntity.Name);

        if (newEntity.Description != null)
            _metaDataSystem.SetEntityDescription(entity, newEntity.Description);
    }
}

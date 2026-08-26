using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.CosmicCult.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Content.Shared.Physics;
using Content.Shared.SubFloor;
using Content.Shared.Pinpointer;
using Content.Shared.Mind.Components;
using Robust.Server.GameObjects;

namespace Content.Server.CosmicCult;

public sealed class CosmicBreachSystem : EntitySystem
{
	[Dependency] private IRobustRandom _random = default!;
	[Dependency] private MapSystem _mapSystem = default!;
	[Dependency] private AtmosphereSystem _atmosphere = default!;
	[Dependency] private SharedTransformSystem _transformSystem = default!;


	//Arbitrary values for safe temperature and pressure ranges.
	//If the location is outside these ranges, it'll fall back to different selection logic.
	private const float MinPressureKpa = 50f;
	private const float MaxPressureKpa = 300f;
	private const float MinTemperatureK = 150f;
	private const float MaxTemperatureK = 300f;

	public Entity<CosmicBreachComponent>? StationBreach(HashSet<Entity<NavMapBeaconComponent>> beacons)
	{
		EntityUid? breach = null;

		var stationBeacons = new List<Entity<NavMapBeaconComponent>>(beacons);
		_random.Shuffle(stationBeacons);
		foreach (var beacon in stationBeacons)
		{
			for (var attempt = 0; attempt < 10; attempt++)
			{
				if (TrySpawnAtBeacon(beacon, out breach))
					break;
			}

			if (breach != null)
				break;
		}

		// Fallback
		if (breach == null)
		{
			foreach (var beacon in beacons)
			{
			    var coords = Transform(beacon).Coordinates;
			    if (!TryResolveGrid(coords, out var gridUid, out var grid))
					continue;

			    ClearBlockingEntities(gridUid, grid, coords);
                breach = Spawn("CosmicBreach", coords);
				break;
			}
		}

		if (breach != null && TryComp<CosmicBreachComponent>(breach, out var breachComp))
			return (breach.Value, breachComp);

		return null;
	}

	private bool TrySpawnAtBeacon(EntityUid beacon, out EntityUid? breach)
	{
        breach = null;

		var beaconCoords = Transform(beacon).Coordinates;
		var offsetX = _random.NextFloat(-10f, 10f);
		var offsetY = _random.NextFloat(-10f, 10f);
		var beaconTransform = Transform(beacon);

		var anchorGrid = beaconTransform.GridUid ?? beaconTransform.MapUid;
		if (anchorGrid == null || !anchorGrid.Value.IsValid())
			return false;

		var targetCoords = new EntityCoordinates(anchorGrid.Value, beaconCoords.Position + new Vector2(offsetX, offsetY));
		if (targetCoords.EntityId == EntityUid.Invalid)
			return false;

		if (!TryFindValid3X3Space(targetCoords, out var centerCoords))
			return false;

        breach = Spawn("CosmicBreach", centerCoords);
		return true;
	}

	private bool TryResolveGrid(EntityCoordinates coords, out EntityUid gridUid, out MapGridComponent grid)
	{
		gridUid = EntityUid.Invalid;
		grid = default!;

		if (TryComp<MapGridComponent>(coords.EntityId, out var directGrid))
		{
			gridUid = coords.EntityId;
			grid = directGrid;
			return true;
		}

		var resolvedGrid = _transformSystem.GetGrid(coords);
		if (resolvedGrid is not { } gridEntity)
			return false;

		if (!TryComp<MapGridComponent>(gridEntity, out var resolvedComp))
			return false;

		gridUid = gridEntity;
		grid = resolvedComp;
		return true;
	}

    private bool TryFindValid3X3Space(EntityCoordinates center, out EntityCoordinates validCenter)
	{
		validCenter = EntityCoordinates.Invalid;

	    if (!TryResolveGrid(center, out var gridUid, out var grid))
		    return false;
	    var centerTile = _mapSystem.TileIndicesFor(gridUid, grid, center);

	    for (var x = -5; x <= 5; x++)
	    {
		    for (var y = -5; y <= 5; y++)
		    {
			    var testTile = new Vector2i(centerTile.X + x, centerTile.Y + y);

			    if (IsValid3X3Space(gridUid, grid, testTile))
			    {
				    validCenter = _mapSystem.GridTileToLocal(gridUid, grid, testTile);
				    return true;
			    }
		    }
	    }

		return false;
	}

	private bool IsValid3X3Space(EntityUid gridUid, MapGridComponent grid, Vector2i center)
	{
		// Check a 3x3 area centered on the candidate tile
		for (var x = -1; x <= 1; x++)
		{
			for (var y = -1; y <= 1; y++)
			{
				var checkTile = new Vector2i(center.X + x, center.Y + y);

				if (!IsTileValid(gridUid, grid, checkTile))
					return false;
			}
		}

		return true;
	}

	private bool IsTileValid(EntityUid gridUid, MapGridComponent grid, Vector2i tile)
	{
        // Check if tile exists and is not space
	    var tileRef = _mapSystem.GetTileRef(gridUid, grid, tile);
	    if (tileRef.Tile.IsEmpty)
		    return false;

	    var mapUid = Transform(gridUid).MapUid;
	    var mixture = _atmosphere.GetTileMixture(gridUid, mapUid, tile, excite: false);
	    if (mixture == null)
		    return false;

	    if (mixture.Pressure < MinPressureKpa || mixture.Pressure > MaxPressureKpa)
		    return false;
	    if (mixture.Temperature < MinTemperatureK || mixture.Temperature > MaxTemperatureK)
			    return false;

		// Check for blocking entities
		var anchored = new HashSet<EntityUid>(_mapSystem.GetAnchoredEntities(gridUid, grid, tile));
        anchored.RemoveWhere(entity => HasComp<SubFloorHideComponent>(entity));
		foreach (var entity in anchored)
		{
            if (HasComp<CosmicBreachComponent>(entity))
                return false;

			if (TryComp<PhysicsComponent>(entity, out var physics))
			{
				var blockingLayers = CollisionGroup.Impassable | CollisionGroup.WallLayer | CollisionGroup.GlassLayer | CollisionGroup.FullTileLayer | CollisionGroup.AirlockLayer | CollisionGroup.GlassAirlockLayer;
				if ((physics.CollisionLayer & (int)blockingLayers) != 0)
					return false;
			}
		}
		return true;
	}

	private void ClearBlockingEntities(EntityUid gridUid, MapGridComponent grid, EntityCoordinates center)
	{
		var centerTile = _mapSystem.TileIndicesFor(gridUid, grid, center);

		// Always a 3x3, so just clear that area
		for (var x = -1; x <= 1; x++)
		{
			for (var y = -1; y <= 1; y++)
			{
				var tileIndices = new Vector2i(centerTile.X + x, centerTile.Y + y);
				var anchored = _mapSystem.GetAnchoredEntities(gridUid, grid, tileIndices);

				foreach (var entity in anchored)
				{
					// Safety check, never delete a player.
					if (TryComp<MindContainerComponent>(entity, out var mind) && mind.Mind != null)
						continue;

					// Destroy walls and other blockers so it doesn't spawn inside a wall.
					if (TryComp<PhysicsComponent>(entity, out var physics))
					{
						var blockingLayers = CollisionGroup.Impassable | CollisionGroup.WallLayer | CollisionGroup.FullTileLayer | CollisionGroup.AirlockLayer;
						if ((physics.CollisionLayer & (int)blockingLayers) != 0)
							QueueDel(entity);
					}
				}
			}
		}
	}
}


// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using System.Collections.Generic;
using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.GameTicking.Rules;
using Content.Shared.BloodCult.Components;
using Content.Shared.BloodCult;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.FixedPoint;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.SubFloor;
using Content.Shared.Pinpointer;
using Content.Shared.Mind.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.BloodCult.EntitySystems;

/// <summary>
/// Handles setting up the final Blood Cult summoning ritual site.
/// Finds a valid 3x3 space near a departmental beacon, replaces flooring, and spawns the rift with runes.
/// </summary>
public sealed class BloodCultRiftSetupSystem : EntitySystem
{
	[Dependency] private readonly IRobustRandom _random = default!;
	[Dependency] private readonly MapSystem _mapSystem = default!;
	[Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
	[Dependency] private readonly AtmosphereSystem _atmosphere = default!;
	[Dependency] private readonly SharedTransformSystem _transformSystem = default!;
	[Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

	//Arbitrary values for safe temperature and pressure ranges.
	//If the location is outside these ranges, it'll fallback to a different site selection logic for the blood anomaly.
	private const float MinPressureKpa = 50f;
	private const float MaxPressureKpa = 300f;
	private const float MinTemperatureK = 150f;
	private const float MaxTemperatureK = 300f;

	/// <summary>
	/// Attempts to set up the final summoning ritual site.
	/// Returns the rift entity if successful, null otherwise.
	/// </summary>
	public EntityUid? TrySetupRitualSite(BloodCultRuleComponent cultRule)
	{
		// 1. Select a random departmental beacon
		var beacons = GetDepartmentalBeacons();
		if (beacons.Count == 0)
			return null;

		EntityUid? rift = null;
		WeakVeilLocation? finalLocation = null;

		// Try random locations near each beacon
		var shuffledBeacons = new List<EntityUid>(beacons);
		_random.Shuffle(shuffledBeacons);

		foreach (var beacon in shuffledBeacons)
		{
			for (var attempt = 0; attempt < 10; attempt++)
			{
				if (TrySpawnAtBeacon(beacon, out rift, out _, out finalLocation))
					break;
			}

			if (rift != null)
				break;
		}

		// Fallback: try around cultists
		if (rift == null)
		{
		var cultists = CollectCultists();
			_random.Shuffle(cultists);
			foreach (var cultist in cultists)
			{
				var cultistXform = Transform(cultist);

			if (TryFindValid3x3Space(cultistXform.Coordinates, out var centerCoords, out var gridUid, out var grid))
				{
					ReplaceFlooring(gridUid, grid, centerCoords);
					rift = SpawnRiftAndRunes(centerCoords);
					var meta = MetaData(cultist);
				var riftCoords = rift.HasValue ? Transform(rift.Value).Coordinates : centerCoords;
				finalLocation = new WeakVeilLocation(meta.EntityName, cultist, meta.EntityPrototype?.ID ?? string.Empty, riftCoords, 3.0f);
					break;
				}
			}
		}

		// Fallback: force spawn on a cultist
		if (rift == null)
		{
		var cultists = CollectCultists();
			foreach (var cultist in cultists)
			{
				var cultistXform = Transform(cultist);

			var coords = cultistXform.Coordinates;
			if (!TryResolveGrid(coords, out var gridUid, out var grid))
					continue;
			// Clear all walls and blockers. It's only meant as a fallback if all normal attempts fail.
			ClearBlockingEntities(gridUid, grid, coords);
			ReplaceFlooring(gridUid, grid, coords);
				rift = SpawnRiftAndRunes(coords);
				var meta = MetaData(cultist);
			var riftCoords = rift.HasValue ? Transform(rift.Value).Coordinates : coords;
			finalLocation = new WeakVeilLocation(meta.EntityName, cultist, meta.EntityPrototype?.ID ?? string.Empty, riftCoords, 3.0f);
				break;
			}
		}

		// Fallback: force spawn on a beacon
		if (rift == null)
		{
			foreach (var beacon in beacons)
			{
			var coords = Transform(beacon).Coordinates;
			if (!TryResolveGrid(coords, out var gridUid, out var grid))
					continue;
			// Clear all walls and blockers. It's only meant as a fallback if all normal attempts fail.
			ClearBlockingEntities(gridUid, grid, coords);
			ReplaceFlooring(gridUid, grid, coords);
				rift = SpawnRiftAndRunes(coords);
				var meta = MetaData(beacon);
			var riftCoords = rift.HasValue ? Transform(rift.Value).Coordinates : coords;
			finalLocation = new WeakVeilLocation(meta.EntityName, beacon, meta.EntityPrototype?.ID ?? string.Empty, riftCoords, 3.0f);
				break;
			}
		}

		if (rift != null && finalLocation != null)
		{
			cultRule.LocationForSummon = finalLocation;
			return rift;
		}

		return null;
	}

	private bool TrySpawnAtBeacon(EntityUid beacon, out EntityUid? rift, out EntityCoordinates? coords, out WeakVeilLocation? location)
	{
		rift = null;
		coords = null;
		location = null;

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

		if (!TryFindValid3x3Space(targetCoords, out var centerCoords, out var gridUid, out var grid))
			return false;

		ReplaceFlooring(gridUid, grid, centerCoords);
		rift = SpawnRiftAndRunes(centerCoords);

		var beaconMeta = MetaData(beacon);
		var locationName = beaconMeta.EntityPrototype?.EditorSuffix ?? beaconMeta.EntityPrototype?.Name ?? beaconMeta.EntityName;
		var protoId = beaconMeta.EntityPrototype?.ID ?? string.Empty;
		coords = rift.HasValue ? Transform(rift.Value).Coordinates : centerCoords;
		location = new WeakVeilLocation(locationName, beacon, protoId, coords.Value, 3.0f);
		return true;
	}

	private bool TryResolveGrid(EntityCoordinates coords, out EntityUid gridUid, out MapGridComponent grid)
	{
		gridUid = EntityUid.Invalid;
		grid = default!;

		if (EntityManager.TryGetComponent<MapGridComponent>(coords.EntityId, out var directGrid) && directGrid != null)
		{
			gridUid = coords.EntityId;
			grid = directGrid;
			return true;
		}

		var resolvedGrid = _transformSystem.GetGrid(coords);
		if (resolvedGrid is not { } gridEntity)
			return false;

		if (!EntityManager.TryGetComponent<MapGridComponent>(gridEntity, out var resolvedComp) || resolvedComp == null)
			return false;

		gridUid = gridEntity;
		grid = resolvedComp;
		return true;
	}

	private List<EntityUid> CollectCultists()
	{
		var cultists = new List<EntityUid>();
		var query = EntityQueryEnumerator<BloodCultistComponent>();
		while (query.MoveNext(out var cultistUid, out _))
		{
			cultists.Add(cultistUid);
		}

		return cultists;
	}

	private List<EntityUid> GetDepartmentalBeacons()
	{
		var beacons = new List<EntityUid>();
		var query = EntityQueryEnumerator<NavMapBeaconComponent, MetaDataComponent>();

		while (query.MoveNext(out var beaconUid, out var navBeacon, out var meta))
		{
			if (meta.EntityPrototype != null &&
				BloodCultRuleComponent.PossibleVeilLocations.Contains(meta.EntityPrototype.ID))
			{
				beacons.Add(beaconUid);
			}
		}

		return beacons;
	}

private bool TryFindValid3x3Space(EntityCoordinates center, out EntityCoordinates validCenter,
	out EntityUid gridUid, out MapGridComponent grid)
	{
		validCenter = EntityCoordinates.Invalid;
		gridUid = EntityUid.Invalid;
	grid = default!;

	if (!TryResolveGrid(center, out gridUid, out grid))
		return false;
	var centerTile = _mapSystem.TileIndicesFor(gridUid, grid, center);

	// Search in a 10x10 area around the beacon
	for (var x = -5; x <= 5; x++)
	{
		for (var y = -5; y <= 5; y++)
		{
			var testTile = new Vector2i(centerTile.X + x, centerTile.Y + y);

			if (IsValid3x3Space(gridUid, grid, testTile))
			{
				validCenter = _mapSystem.GridTileToLocal(gridUid, grid, testTile);
				return true;
			}
		}
	}

		return false;
	}

	private bool IsValid3x3Space(EntityUid gridUid, MapGridComponent grid, Vector2i center)
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

		// Check for blocking entities (walls, etc)
		var anchored = _mapSystem.GetAnchoredEntities(gridUid, grid, tile);
		foreach (var entity in anchored)
		{
			// Allow subfloor items (cables, pipes)
			if (HasComp<SubFloorHideComponent>(entity))
				continue;

			// Block on walls or dense structures
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

	private void ReplaceFlooring(EntityUid gridUid, MapGridComponent grid, EntityCoordinates center)
	{
		var centerTile = _mapSystem.TileIndicesFor(gridUid, grid, center);

		// Replace 3x3 area with reinforced exterior hull flooring (centered around the rift position)
		// Todo: Get a cooler looking bloodcult floor tile. 
		// I just want to make sure they can't de-grid the anomaly because that'd break the code. And it needs to have adjacent tiles open because it needs those for offering runes to work.
		var reinforcedTileDef = (ContentTileDefinition)_tileDefManager["FloorHullReinforced"];
		var reinforcedTile = new Tile(reinforcedTileDef.TileId);

		for (var x = -1; x <= 1; x++)
		{
			for (var y = -1; y <= 1; y++)
			{
				var tileIndices = new Vector2i(centerTile.X + x, centerTile.Y + y);
				_mapSystem.SetTile(gridUid, grid, tileIndices, reinforcedTile);
			}
		}
	}

	private EntityUid SpawnRiftAndRunes(EntityCoordinates center)
	{
		// Spawn rift at center
		var rift = Spawn("BloodCultRift", center);
		var riftComp = EnsureComp<BloodCultRiftComponent>(rift);
		riftComp.SummoningRunes.Clear();
		riftComp.OfferingRunes.Clear();

		// Spawn the final rift rune sprite at the center (same location as rift)
		// Same size as TearVeilRune, with constant animation (no drawing animation)
		var finalRune = Spawn("FinalRiftRune", center);
		var finalRuneComp = EnsureComp<FinalSummoningRuneComponent>(finalRune);
		finalRuneComp.RiftUid = rift;

		// Remove CleanableRune and Reactive components - FinalRiftRune should not be cleanable
		// These are inherited from BaseBloodCultRune but we don't want them for the final rune
		RemComp<CleanableRuneComponent>(finalRune);
		RemComp<ReactiveComponent>(finalRune);

		// Track the rune for chanting and offerings
		riftComp.SummoningRunes.Add(finalRune);
		riftComp.OfferingRunes.Add(finalRune);

		// Pre-fills the blood pool with unholy blood.
		// This makes it so the anomaly spills blood onto the floor when it pulses, rather than taking a while to fill up. It'll make a slowly-growing ocean of blood.
		if (_solutionContainer.TryGetSolution(rift, "sanguine_pool", out var solutionEnt, out var solution))
		{
			var deficit = solution.MaxVolume - solution.Volume;
			if (deficit > FixedPoint2.Zero)
				_solutionContainer.TryAddReagent(solutionEnt.Value, "UnholyBlood", deficit, out _);
		}

		return rift;
	}
}


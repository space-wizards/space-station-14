using System.Linq;
using Content.Server._FTL.AutomatedShip.Components;
using Content.Server._FTL.ShipTracker;
using Content.Server._FTL.ShipTracker.Events;
using Content.Server._FTL.Weapons;
using Content.Server.Atmos.EntitySystems;
using Content.Server.NPC.Components;
using Content.Server.NPC.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._FTL.AutomatedShip.Systems;

/// <summary>
/// This handles AI control
/// </summary>
public sealed partial class AutomatedShipSystem : EntitySystem
{
    [Dependency] private readonly WeaponTargetingSystem _weaponTargetingSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly NpcFactionSystem _npcFactionSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutomatedShipComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<AutomatedShipComponent, ShipDamagedEvent>(OnShipDamaged);
    }

    private void OnInit(EntityUid uid, AutomatedShipComponent component, ComponentInit args)
    {
        EnsureComp<ActiveAutomatedShipComponent>(uid);
    }

    private bool TryFindRandomTile(EntityUid targetGrid, out Vector2i tile, out EntityCoordinates targetCoords)
    {
        tile = default;

        targetCoords = EntityCoordinates.Invalid;

        if (!TryComp<MapGridComponent>(targetGrid, out var gridComp))
            return false;

        var found = false;
        var (gridPos, _, gridMatrix) = _transformSystem.GetWorldPositionRotationMatrix(targetGrid);
        var gridBounds = gridMatrix.TransformBox(gridComp.LocalAABB);

        for (var i = 0; i < 10; i++)
        {
            var randomX = _random.Next((int) gridBounds.Left, (int) gridBounds.Right);
            var randomY = _random.Next((int) gridBounds.Bottom, (int) gridBounds.Top);

            tile = new Vector2i(randomX - (int) gridPos.X, randomY - (int) gridPos.Y);
            if (_atmosphereSystem.IsTileSpace(targetGrid, Transform(targetGrid).MapUid, tile,
                    mapGridComp: gridComp)
                || _atmosphereSystem.IsTileAirBlocked(targetGrid, tile, mapGridComp: gridComp))
            {
                continue;
            }

            found = true;
            targetCoords = gridComp.GridTileToLocal(tile);
            break;
        }

        return found;
    }

    public void AutomatedShipJump()
    {
        // TODO: Make all ships jump to a random point in range
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveAutomatedShipComponent, AutomatedShipComponent, TransformComponent, ShipTrackerComponent>();
        while (query.MoveNext(out var entity, out var activeComponent, out var aiComponent, out var transformComponent, out var aiTrackerComponent))
        {
            // makes sure it's on the same map, not the same grid, and is hostile
            var transform = transformComponent;

            var hostileShips = EntityQuery<ShipTrackerComponent>().Where(shipTrackerComponent =>
            {
                var owner = shipTrackerComponent.Owner;
                var otherTransform = Transform(owner);

                Log.Debug($"Same map: {otherTransform.MapID == transform.MapID}, Different grid: {otherTransform.GridUid != transform.GridUid}, Hostile: {_npcFactionSystem.IsFactionHostile(aiTrackerComponent.Faction,
                    shipTrackerComponent.Faction)}");

                return otherTransform.MapID == transform.MapID && otherTransform.GridUid != transform.GridUid &&
                       (_npcFactionSystem.IsFactionHostile(aiTrackerComponent.Faction,
                           shipTrackerComponent.Faction) ||
                       aiComponent.HostileShips.Contains(owner));
            }).ToList();

            if (hostileShips.Count <= 0)
                continue;

            var mainShip = _random.Pick(hostileShips).Owner;

            // I seperated these into partial systems because I hate large line counts!!!
            switch (aiComponent.AiState)
            {
                case AutomatedShipComponent.AiStates.Cruising:
                {
                    if (hostileShips.Count > 0)
                    {
                        aiComponent.AiState = AutomatedShipComponent.AiStates.Fighting;
                        Log.Debug("Hostile ship inbound!");
                    }
                    break;
                }
                case AutomatedShipComponent.AiStates.Fighting:
                {
                    if (hostileShips.Count <= 0)
                    {
                        aiComponent.AiState = AutomatedShipComponent.AiStates.Cruising;
                        Log.Debug("Lack of a hostile ship.");
                        break;
                    }
                    PerformCombat(entity,
                        activeComponent,
                        aiComponent,
                        transformComponent,
                        aiTrackerComponent,
                        mainShip);
                    break;
                }
                default:
                {
                    Log.Fatal("Non-existent AI state!");
                    break;
                }
            }

            activeComponent.TimeSinceLastAttack += frameTime;
        }
    }
}

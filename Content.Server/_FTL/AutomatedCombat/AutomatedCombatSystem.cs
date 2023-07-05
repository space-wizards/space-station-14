using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._FTL.ShipHealth;
using Content.Server._FTL.Weapons;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Station.Components;
using Content.Shared.Atmos;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._FTL.AutomatedCombat;

/// <summary>
/// This handles...
/// </summary>
public sealed class AutomatedCombatSystem : EntitySystem
{
    [Dependency] private readonly WeaponTargetingSystem _weaponTargetingSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutomatedCombatComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, AutomatedCombatComponent component, ComponentInit args)
    {
        EnsureComp<ActiveAutomatedCombatComponent>(uid);
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

    private List<EntityUid> GetWeaponsOnGrid(EntityUid gridUid)
    {
        var weapons = new List<EntityUid>();
        var query = EntityQueryEnumerator<FTLWeaponComponent, TransformComponent>();
        while (query.MoveNext(out var entity, out var weapon, out var xform))
        {
            if (xform.GridUid == gridUid)
            {
                weapons.Add(entity);
            }
        }

        return weapons;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveAutomatedCombatComponent, AutomatedCombatComponent>();
        while (query.MoveNext(out var entity, out var activeComponent, out var component))
        {
            activeComponent.TimeSinceLastAttack += frameTime;
            if (activeComponent.TimeSinceLastAttack >= component.AttackRepetition)
            {
                var mainShips = EntityQuery<MainCharacterShipComponent>().ToList();

                if (mainShips.Count <= 0)
                    break;

                var mainShip = _random.Pick(mainShips).Owner;

                var weapons = GetWeaponsOnGrid(entity);
                var weapon = _random.Pick(weapons);

                if (TryComp<FTLWeaponComponent>(weapon, out var weaponComponent) && TryFindRandomTile(mainShip, out _, out var coordinates))
                {
                    activeComponent.TimeSinceLastAttack = 0;
                    Log.Debug(coordinates.ToString());
                    _weaponTargetingSystem.TryFireWeapon(weapon, weaponComponent, mainShip, coordinates, null);
                }
            }
        }
    }
}

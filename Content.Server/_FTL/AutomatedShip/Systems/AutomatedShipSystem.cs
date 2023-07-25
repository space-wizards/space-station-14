using System.Linq;
using Content.Server._FTL.AutomatedShip.Components;
using Content.Server._FTL.ShipTracker;
using Content.Server._FTL.ShipTracker.Events;
using Content.Server._FTL.ShipTracker.Systems;
using Content.Server._FTL.Weapons;
using Content.Server.NPC.Systems;
using Robust.Shared.Random;

namespace Content.Server._FTL.AutomatedShip.Systems;

/// <summary>
/// This handles AI control
/// </summary>
public sealed partial class AutomatedShipSystem : EntitySystem
{
    [Dependency] private readonly WeaponTargetingSystem _weaponTargetingSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ShipTrackerSystem _shipTracker = default!;
    [Dependency] private readonly NpcFactionSystem _npcFactionSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutomatedShipComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<AutomatedShipComponent, ShipDamagedEvent>(OnShipDamaged);
    }

    private void OnInit(EntityUid uid, AutomatedShipComponent component, ComponentInit args)
    {
        EnsureComp<ActiveAutomatedShipComponent>(uid);
        UpdateName(uid, component);
    }

    public void UpdateName(EntityUid uid, AutomatedShipComponent component)
    {
        var meta = MetaData(uid);
        var tag = component.AiState == AutomatedShipComponent.AiStates.Cruising ? Loc.GetString("ship-state-tag-neutral") : Loc.GetString("ship-state-tag-hostile");

        // has the tag
        if (meta.EntityName.StartsWith("["))
        {
            _metaDataSystem.SetEntityName(uid,tag + meta.EntityName.Substring(5, meta.EntityName.Length));
            return;
        }
        _metaDataSystem.SetEntityName(uid, tag + meta.EntityName);
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

            UpdateName(entity, aiComponent);

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

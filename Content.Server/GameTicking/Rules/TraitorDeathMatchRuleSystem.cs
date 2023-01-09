using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Server.Hands.Components;
using Content.Server.PDA;
using Content.Server.Players;
using Content.Server.Spawners.Components;
using Content.Server.Store.Components;
using Content.Server.Traitor;
using Content.Server.Traitor.Uplink;
using Content.Server.TraitorDeathMatch.Components;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.PDA;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules;

public sealed class TraitorDeathMatchRuleSystem : GameRuleSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MaxTimeRestartRuleSystem _restarter = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly UplinkSystem _uplink = default!;

    public override string Prototype => "TraitorDeathMatch";

    public string PDAPrototypeName => "CaptainPDA";
    public string BeltPrototypeName => "ClothingBeltJanitorFilled";
    public string BackpackPrototypeName => "ClothingBackpackFilled";

    private bool _safeToEndRound = false;

    private readonly Dictionary<EntityUid, string> _allOriginalNames = new();

    private const string TraitorPrototypeID = "Traitor";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned);
        SubscribeLocalEvent<GhostAttemptHandleEvent>(OnGhostAttempt);
    }

    private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
    {
        if (!RuleAdded)
            return;

        var session = ev.Player;
        var startingBalance = _cfg.GetCVar(CCVars.TraitorDeathMatchStartingBalance);

        // Yup, they're a traitor
        var mind = session.Data.ContentData()?.Mind;
        if (mind == null)
        {
            Logger.ErrorS("preset", "Failed getting mind for TDM player.");
            return;
        }

        var antagPrototype = _prototypeManager.Index<AntagPrototype>(TraitorPrototypeID);
        var traitorRole = new TraitorRole(mind, antagPrototype);
        mind.AddRole(traitorRole);

        // Delete anything that may contain "dangerous" role-specific items.
        // (This includes the PDA, as everybody gets the captain PDA in this mode for true-all-access reasons.)
        if (mind.OwnedEntity is {Valid: true} owned)
        {
            var victimSlots = new[] {"id", "belt", "back"};
            foreach (var slot in victimSlots)
            {
                if(_inventory.TryUnequip(owned, slot, out var entityUid, true, true))
                    Del(entityUid.Value);
            }

            // Replace their items:

            var ownedCoords = Transform(owned).Coordinates;

            //  pda
            var newPDA = Spawn(PDAPrototypeName, ownedCoords);
            _inventory.TryEquip(owned, newPDA, "id", true);

            //  belt
            var newTmp = Spawn(BeltPrototypeName, ownedCoords);
            _inventory.TryEquip(owned, newTmp, "belt", true);

            //  backpack
            newTmp = Spawn(BackpackPrototypeName, ownedCoords);
            _inventory.TryEquip(owned, newTmp, "back", true);

            if (!_uplink.AddUplink(owned, startingBalance))
                return;

            _allOriginalNames[owned] = Name(owned);

            // The PDA needs to be marked with the correct owner.
            var pda = Comp<PDAComponent>(newPDA);
            EntityManager.EntitySysManager.GetEntitySystem<PDASystem>().SetOwner(pda, Name(owned));
            EntityManager.AddComponent<TraitorDeathMatchReliableOwnerTagComponent>(newPDA).UserId = mind.UserId;
        }

        // Finally, it would be preferable if they spawned as far away from other players as reasonably possible.
        if (mind.OwnedEntity != null && FindAnyIsolatedSpawnLocation(mind, out var bestTarget))
        {
            Transform(mind.OwnedEntity.Value).Coordinates = bestTarget;
        }
        else
        {
            // The station is too drained of air to safely continue.
            if (_safeToEndRound)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("traitor-death-match-station-is-too-unsafe-announcement"));
                _restarter.RoundMaxTime = TimeSpan.FromMinutes(1);
                _restarter.RestartTimer();
                _safeToEndRound = false;
            }
        }
    }

    private void OnGhostAttempt(GhostAttemptHandleEvent ev)
    {
        if (!RuleAdded || ev.Handled)
            return;

        ev.Handled = true;

        var mind = ev.Mind;

        if (mind.OwnedEntity is {Valid: true} entity && TryComp(entity, out MobStateComponent? mobState))
        {
            if (_mobStateSystem.IsCritical(entity, mobState))
            {
                // TODO BODY SYSTEM KILL
                var damage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Asphyxiation"), 100);
                Get<DamageableSystem>().TryChangeDamage(entity, damage, true);
            }
            else if (!_mobStateSystem.IsDead(entity,mobState))
            {
                if (HasComp<HandsComponent>(entity))
                {
                    ev.Result = false;
                    return;
                }
            }
        }
        var session = mind.Session;
        if (session == null)
        {
            ev.Result = false;
            return;
        }

        GameTicker.Respawn(session);
        ev.Result = true;
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        if (!RuleAdded)
            return;

        var lines = new List<string>();
        lines.Add(Loc.GetString("traitor-death-match-end-round-description-first-line"));

        foreach (var uplink in EntityManager.EntityQuery<StoreComponent>(true))
        {
            var owner = uplink.AccountOwner;
            if (owner != null && _allOriginalNames.ContainsKey(owner.Value))
            {
                var tcbalance = _uplink.GetTCBalance(uplink);

                lines.Add(Loc.GetString("traitor-death-match-end-round-description-entry",
                    ("originalName", _allOriginalNames[owner.Value]),
                    ("tcBalance", tcbalance)));
            }
        }

        ev.AddLine(string.Join('\n', lines));
    }

    public override void Started()
    {
        _restarter.RoundMaxTime = TimeSpan.FromMinutes(30);
        _restarter.RestartTimer();
        _safeToEndRound = true;
    }

    public override void Ended()
    {
    }

    // It would be nice if this function were moved to some generic helpers class.
    private bool FindAnyIsolatedSpawnLocation(Mind.Mind ignoreMe, out EntityCoordinates bestTarget)
    {
        // Collate people to avoid...
        var existingPlayerPoints = new List<EntityCoordinates>();
        foreach (var player in _playerManager.ServerSessions)
        {
            var avoidMeMind = player.Data.ContentData()?.Mind;
            if ((avoidMeMind == null) || (avoidMeMind == ignoreMe))
                continue;
            var avoidMeEntity = avoidMeMind.OwnedEntity;
            if (avoidMeEntity == null)
                continue;
            if (TryComp(avoidMeEntity.Value, out MobStateComponent? mobState))
            {
                // Does have mob state component; if critical or dead, they don't really matter for spawn checks
                if (_mobStateSystem.IsCritical(avoidMeEntity.Value, mobState) || _mobStateSystem.IsDead(avoidMeEntity.Value, mobState))
                    continue;
            }
            else
            {
                // Doesn't have mob state component. Assume something interesting is going on and don't count this as someone to avoid.
                continue;
            }
            existingPlayerPoints.Add(Transform(avoidMeEntity.Value).Coordinates);
        }

        // Iterate over each possible spawn point, comparing to the existing player points.
        // On failure, the returned target is the location that we're already at.
        var bestTargetDistanceFromNearest = -1.0f;
        // Need the random shuffle or it stuffs the first person into Atmospherics pretty reliably
        var ents = EntityManager.EntityQuery<SpawnPointComponent>().Select(x => x.Owner).ToList();
        _robustRandom.Shuffle(ents);
        var foundATarget = false;
        bestTarget = EntityCoordinates.Invalid;

        foreach (var entity in ents)
        {
            var transform = Transform(entity);

            if (transform.GridUid == null || transform.MapUid == null)
                continue;

            var position = _transformSystem.GetGridOrMapTilePosition(entity, transform);

            if (!_atmosphereSystem.IsTileMixtureProbablySafe(transform.GridUid.Value, transform.MapUid.Value, position))
                continue;

            var distanceFromNearest = float.PositiveInfinity;
            foreach (var existing in existingPlayerPoints)
            {
                if (Transform(entity).Coordinates.TryDistance(EntityManager, existing, out var dist))
                    distanceFromNearest = Math.Min(distanceFromNearest, dist);
            }
            if (bestTargetDistanceFromNearest < distanceFromNearest)
            {
                bestTarget = Transform(entity).Coordinates;
                bestTargetDistanceFromNearest = distanceFromNearest;
                foundATarget = true;
            }
        }
        return foundATarget;
    }

}

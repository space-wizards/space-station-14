using System.Linq;
using Content.Server.Administration.Commands;
using Content.Server.Cargo.Systems;
using Content.Server.Chat.Managers;
using Content.Server.Preferences.Managers;
using Content.Server.Spawners.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Robust.Shared.Enums;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// This handles the Pirates minor antag, which is designed to coincide with other modes on occasion.
/// </summary>
public sealed class PiratesRuleSystem : GameRuleSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawningSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly PricingSystem _pricingSystem = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly NamingSystem _namingSystem = default!;

    [ViewVariables]
    private List<Mind.Mind> _pirates = new();
    [ViewVariables]
    private EntityUid _pirateShip = EntityUid.Invalid;
    [ViewVariables]
    private HashSet<EntityUid> _initialItems = new();
    [ViewVariables]
    private double _initialShipValue;

    public override string Prototype => "Pirates";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RulePlayerSpawningEvent>(OnPlayerSpawningEvent);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndTextEvent);
    }

    private void OnRoundEndTextEvent(RoundEndTextAppendEvent ev)
    {
        if (!RuleAdded)
            return;

        if (Deleted(_pirateShip))
        {
            // Major loss, the ship somehow got annihilated.
            ev.AddLine(Loc.GetString("pirates-no-ship"));
        }
        else
        {

            List<(double, EntityUid)> mostValuableThefts = new();

            var finalValue = _pricingSystem.AppraiseGrid(_pirateShip, uid =>
            {
                foreach (var mind in _pirates)
                {
                    if (mind.CurrentEntity == uid)
                        return false; // Don't appraise the pirates twice, we count them in separately.
                }
                return true;
            }, (uid, price) =>
            {
                if (_initialItems.Contains(uid))
                    return;

                mostValuableThefts.Add((price, uid));
                mostValuableThefts.Sort((i1, i2) => i2.Item1.CompareTo(i1.Item1));
                if (mostValuableThefts.Count > 5)
                    mostValuableThefts.Pop();
            });

            foreach (var mind in _pirates)
            {
                if (mind.CurrentEntity is not null)
                    finalValue += _pricingSystem.GetPrice(mind.CurrentEntity.Value);
            }

            var score = finalValue - _initialShipValue;

            ev.AddLine(Loc.GetString("pirates-final-score", ("score", $"{score:F2}")));
            ev.AddLine(Loc.GetString("pirates-final-score-2", ("finalPrice", $"{finalValue:F2}")));

            ev.AddLine("");
            ev.AddLine(Loc.GetString("pirates-most-valuable"));

            foreach (var (price, obj) in mostValuableThefts)
            {
                ev.AddLine(Loc.GetString("pirates-stolen-item-entry", ("entity", obj), ("credits", $"{price:F2}")));
            }

            if (mostValuableThefts.Count == 0)
                ev.AddLine(Loc.GetString("pirates-stole-nothing"));
        }

        ev.AddLine("");
        ev.AddLine(Loc.GetString("pirates-list-start"));
        foreach (var pirates in _pirates)
        {
            ev.AddLine($"- {pirates.CharacterName} ({pirates.Session?.Name})");
        }
    }

    public override void Started() { }

    public override void Ended() { }

    private void OnPlayerSpawningEvent(RulePlayerSpawningEvent ev)
    {
        // Forgive me for copy-pasting nukies.
        if (!RuleAdded)
        {
            return;
        }

        _pirates.Clear();
        _initialItems.Clear();

        // Between 1 and <max pirate count>: needs at least n players per op.
        var numOps = Math.Max(1,
            (int)Math.Min(
                Math.Floor((double)ev.PlayerPool.Count / _cfg.GetCVar(CCVars.PiratesPlayersPerOp)), _cfg.GetCVar(CCVars.PiratesMaxOps)));
        var ops = new IPlayerSession[numOps];
        for (var i = 0; i < numOps; i++)
        {
            ops[i] = _random.PickAndTake(ev.PlayerPool);
        }

        var map = "/Maps/pirate.yml";
        var xformQuery = GetEntityQuery<TransformComponent>();

        var aabbs = _stationSystem.Stations.SelectMany(x =>
            Comp<StationDataComponent>(x).Grids.Select(x => xformQuery.GetComponent(x).WorldMatrix.TransformBox(_mapManager.GetGridComp(x).LocalAABB))).ToArray();

        var aabb = aabbs[0];

        for (var i = 1; i < aabbs.Length; i++)
        {
            aabb.Union(aabbs[i]);
        }

        var gridId = _map.LoadGrid(GameTicker.DefaultMap, map, new MapLoadOptions
        {
            Offset = aabb.Center + MathF.Max(aabb.Height / 2f, aabb.Width / 2f) * 2.5f
        });

        if (!gridId.HasValue)
        {
            Logger.ErrorS("pirates", $"Gridid was null when loading \"{map}\", aborting.");
            foreach (var session in ops)
            {
                ev.PlayerPool.Add(session);
            }
            return;
        }

        _pirateShip = gridId.Value;

        // TODO: Loot table or something
        var pirateGear = _prototypeManager.Index<StartingGearPrototype>("PirateGear"); // YARRR

        var spawns = new List<EntityCoordinates>();

        // Forgive me for hardcoding prototypes
        foreach (var (_, meta, xform) in EntityQuery<SpawnPointComponent, MetaDataComponent, TransformComponent>(true))
        {
            if (meta.EntityPrototype?.ID != "SpawnPointPirates" || xform.ParentUid != _pirateShip) continue;

            spawns.Add(xform.Coordinates);
        }

        if (spawns.Count == 0)
        {
            spawns.Add(Transform(_pirateShip).Coordinates);
            Logger.WarningS("pirates", $"Fell back to default spawn for pirates!");
        }

        for (var i = 0; i < ops.Length; i++)
        {
            var sex = _random.Prob(0.5f) ? Sex.Male : Sex.Female;
            var gender = sex == Sex.Male ? Gender.Male : Gender.Female;

            var name = _namingSystem.GetName("Human", gender);

            var session = ops[i];
            var newMind = new Mind.Mind(session.UserId)
            {
                CharacterName = name
            };
            newMind.ChangeOwningPlayer(session.UserId);

            var mob = Spawn("MobHuman", _random.Pick(spawns));
            MetaData(mob).EntityName = name;

            newMind.TransferTo(mob);
            var profile = _prefs.GetPreferences(session.UserId).SelectedCharacter as HumanoidCharacterProfile;
            _stationSpawningSystem.EquipStartingGear(mob, pirateGear, profile);

            _pirates.Add(newMind);

            GameTicker.PlayerJoinGame(session);
        }

        _initialShipValue = _pricingSystem.AppraiseGrid(_pirateShip, uid =>
        {
            _initialItems.Add(uid);
            return true;
        }); // Include the players in the appraisal.
    }

    //Forcing one player to be a pirate.
    public void MakePirate(Mind.Mind mind)
    {
        if (!mind.OwnedEntity.HasValue)
            return;
        SetOutfitCommand.SetOutfit(mind.OwnedEntity.Value, "PirateGear", EntityManager);
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        if (!RuleAdded)
            return;

        var minPlayers = _cfg.GetCVar(CCVars.PiratesMinPlayers);
        if (!ev.Forced && ev.Players.Length < minPlayers)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("nukeops-not-enough-ready-players", ("readyPlayersCount", ev.Players.Length), ("minimumPlayers", minPlayers)));
            ev.Cancel();
            return;
        }

        if (ev.Players.Length == 0)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("nukeops-no-one-ready"));
            ev.Cancel();
        }
    }
}

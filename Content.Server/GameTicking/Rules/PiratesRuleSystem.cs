using System.Linq;
using System.Numerics;
using Content.Server.Administration.Commands;
using Content.Server.Cargo.Systems;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Preferences.Managers;
using Content.Server.Spawners.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.NPC.Systems;
using Content.Shared.CCVar;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// This handles the Pirates minor antag, which is designed to coincide with other modes on occasion.
/// </summary>
public sealed class PiratesRuleSystem : GameRuleSystem<PiratesRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawningSystem = default!;
    [Dependency] private readonly PricingSystem _pricingSystem = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly NamingSystem _namingSystem = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RulePlayerSpawningEvent>(OnPlayerSpawningEvent);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndTextEvent);
        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
    }

    private void OnRoundEndTextEvent(RoundEndTextAppendEvent ev)
    {
        var query = EntityQueryEnumerator<PiratesRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var pirates, out var gameRule))
        {
            if (Deleted(pirates.PirateShip))
            {
                // Major loss, the ship somehow got annihilated.
                ev.AddLine(Loc.GetString("pirates-no-ship"));
            }
            else
            {
                List<(double, EntityUid)> mostValuableThefts = new();

                var comp1 = pirates;
                var finalValue = _pricingSystem.AppraiseGrid(pirates.PirateShip, uid =>
                {
                    foreach (var mindId in comp1.Pirates)
                    {
                        if (TryComp(mindId, out MindComponent? mind) && mind.CurrentEntity == uid)
                            return false; // Don't appraise the pirates twice, we count them in separately.
                    }

                    return true;
                }, (uid, price) =>
                {
                    if (comp1.InitialItems.Contains(uid))
                        return;

                    mostValuableThefts.Add((price, uid));
                    mostValuableThefts.Sort((i1, i2) => i2.Item1.CompareTo(i1.Item1));
                    if (mostValuableThefts.Count > 5)
                        mostValuableThefts.Pop();
                });

                foreach (var mindId in pirates.Pirates)
                {
                    if (TryComp(mindId, out MindComponent? mind) && mind.CurrentEntity is not null)
                        finalValue += _pricingSystem.GetPrice(mind.CurrentEntity.Value);
                }

                var score = finalValue - pirates.InitialShipValue;

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
            foreach (var pirate in pirates.Pirates)
            {
                if (TryComp(pirate, out MindComponent? mind))
                {
                    ev.AddLine($"- {mind.CharacterName} ({mind.Session?.Name})");
                }
            }
        }
    }

    private void OnPlayerSpawningEvent(RulePlayerSpawningEvent ev)
    {
        var query = EntityQueryEnumerator<PiratesRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var pirates, out var gameRule))
        {
            // Forgive me for copy-pasting nukies.
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                return;

            pirates.Pirates.Clear();
            pirates.InitialItems.Clear();

            // Between 1 and <max pirate count>: needs at least n players per op.
            var numOps = Math.Max(1,
                (int) Math.Min(
                    Math.Floor((double) ev.PlayerPool.Count / _cfg.GetCVar(CCVars.PiratesPlayersPerOp)),
                    _cfg.GetCVar(CCVars.PiratesMaxOps)));
            var ops = new ICommonSession[numOps];
            for (var i = 0; i < numOps; i++)
            {
                ops[i] = _random.PickAndTake(ev.PlayerPool);
            }

            var map = "/Maps/Shuttles/pirate.yml";
            var xformQuery = GetEntityQuery<TransformComponent>();

            var aabbs = EntityQuery<StationDataComponent>().SelectMany(x =>
                    x.Grids.Select(x =>
                        xformQuery.GetComponent(x).WorldMatrix.TransformBox(_mapManager.GetGridComp(x).LocalAABB)))
                .ToArray();

            var aabb = aabbs[0];

            for (var i = 1; i < aabbs.Length; i++)
            {
                aabb.Union(aabbs[i]);
            }

            // (Not commented?)
            var a = MathF.Max(aabb.Height / 2f, aabb.Width / 2f) * 2.5f;

            var gridId = _map.LoadGrid(GameTicker.DefaultMap, map, new MapLoadOptions
            {
                Offset = aabb.Center + new Vector2(a, a),
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

            pirates.PirateShip = gridId.Value;

            // TODO: Loot table or something
            var pirateGear = _prototypeManager.Index<StartingGearPrototype>("PirateGear"); // YARRR

            var spawns = new List<EntityCoordinates>();

            // Forgive me for hardcoding prototypes
            foreach (var (_, meta, xform) in
                     EntityQuery<SpawnPointComponent, MetaDataComponent, TransformComponent>(true))
            {
                if (meta.EntityPrototype?.ID != "SpawnPointPirates" || xform.ParentUid != pirates.PirateShip)
                    continue;

                spawns.Add(xform.Coordinates);
            }

            if (spawns.Count == 0)
            {
                spawns.Add(Transform(pirates.PirateShip).Coordinates);
                Logger.WarningS("pirates", $"Fell back to default spawn for pirates!");
            }

            for (var i = 0; i < ops.Length; i++)
            {
                var sex = _random.Prob(0.5f) ? Sex.Male : Sex.Female;
                var gender = sex == Sex.Male ? Gender.Male : Gender.Female;

                var name = _namingSystem.GetName("Human", gender);

                var session = ops[i];
                var newMind = _mindSystem.CreateMind(session.UserId, name);
                _mindSystem.SetUserId(newMind, session.UserId);

                var mob = Spawn("MobHuman", _random.Pick(spawns));
                _metaData.SetEntityName(mob, name);

                _mindSystem.TransferTo(newMind, mob);
                var profile = _prefs.GetPreferences(session.UserId).SelectedCharacter as HumanoidCharacterProfile;
                _stationSpawningSystem.EquipStartingGear(mob, pirateGear, profile);

                _npcFaction.RemoveFaction(mob, "NanoTrasen", false);
                _npcFaction.AddFaction(mob, "Syndicate");

                pirates.Pirates.Add(newMind);

                // Notificate every player about a pirate antagonist role with sound
                _audioSystem.PlayGlobal(pirates.PirateAlertSound, session);

                GameTicker.PlayerJoinGame(session);
            }

            pirates.InitialShipValue = _pricingSystem.AppraiseGrid(pirates.PirateShip, uid =>
            {
                pirates.InitialItems.Add(uid);
                return true;
            }); // Include the players in the appraisal.
        }
    }

    //Forcing one player to be a pirate.
    public void MakePirate(EntityUid mindId, MindComponent mind)
    {
        if (!mind.OwnedEntity.HasValue)
            return;
        SetOutfitCommand.SetOutfit(mind.OwnedEntity.Value, "PirateGear", EntityManager);

        var pirateRule = EntityQuery<PiratesRuleComponent>().FirstOrDefault();
        if (pirateRule == null)
        {
            //todo fuck me this shit is awful
            GameTicker.StartGameRule("Pirates", out var ruleEntity);
            pirateRule = Comp<PiratesRuleComponent>(ruleEntity);
        }

        // Notificate every player about a pirate antagonist role with sound
        if (mind.Session != null)
        {
            _audioSystem.PlayGlobal(pirateRule.PirateAlertSound, mind.Session);
        }
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        var query = EntityQueryEnumerator<PiratesRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var pirates, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                return;

            var minPlayers = _cfg.GetCVar(CCVars.PiratesMinPlayers);
            if (!ev.Forced && ev.Players.Length < minPlayers)
            {
                _chatManager.SendAdminAnnouncement(Loc.GetString("nukeops-not-enough-ready-players",
                    ("readyPlayersCount", ev.Players.Length), ("minimumPlayers", minPlayers)));
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
}

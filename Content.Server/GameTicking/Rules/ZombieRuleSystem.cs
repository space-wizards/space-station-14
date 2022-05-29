using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Disease;
using Content.Server.Nuke;
using Content.Server.Players;
using Content.Server.RoundEnd;
using Content.Server.Spawners.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.MobState;
using Content.Shared.Roles;
using Robust.Server.Maps;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Rules;

public sealed class ZombieRuleSystem : GameRuleSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IMapLoader _mapLoader = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawningSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly DiseaseSystem _diseaseSystem = default!;
    [Dependency] private readonly NukeSystem _nukeSystem = default!;
    [Dependency] private readonly NukeCodeSystem _nukeCodeSystem = default!;

    private const string PatientZeroPrototypeID = "PatientZero";
    private const string InitialZombieVirusPrototype = "ZombieInfection";

    private Dictionary<Mind.Mind, bool> _aliveNukeops = new();
    private bool _opsWon;

    public override string Prototype => "Zombie";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        //SubscribeLocalEvent<RulePlayerSpawningEvent>(OnPlayersSpawning);
        //SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnJobAssigned);
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        if (!Enabled)
            return;

        ev.AddLine("ZOMBIE ROUND OVER TESTETEST");
        ev.AddLine(Loc.GetString("nukeops-list-start"));
        foreach (var nukeop in _aliveNukeops)
        {
            ev.AddLine($"- {nukeop.Key.Session?.Name}");
        }
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        
    }

    private void OnJobAssigned(RulePlayerJobsAssignedEvent ev)
    {
        if (!Enabled)
            return;

        var list = new List<IPlayerSession>(ev.Players);
        var prefList = new List<IPlayerSession>();

        foreach (var player in list)
        {
            if (!ev.Profiles.ContainsKey(player.UserId))
            {
                continue;
            }
            prefList.Add(player); //by default, zombies don't care whether or not you are command or nonantag crew
        }

        var playersPerInfected = _cfg.GetCVar(CCVars.ZombiePlayersPerInfected);
        var maxInfected = _cfg.GetCVar(CCVars.ZombieMaxInfected);

        var numInfected = Math.Max(1,
            (int) Math.Min(
                Math.Floor((double) ev.Players.Length / playersPerInfected), maxInfected));

        for (var i = 0; i < numInfected; i++)
        {
            IPlayerSession zombie; //this isn't actually a zombie, but rather a patient 0. sue me for naming it shitty -emo
            if (prefList.Count == 0)
            {
                if (list.Count == 0)
                {
                    Logger.InfoS("preset", "Insufficient ready players to fill up with patient 0, stopping the selection.");
                    break;
                }
                zombie = _random.PickAndTake(list);
                list.Remove(zombie);
                Logger.InfoS("preset", "Insufficient preferred patient 0, picking at random.");
            }
            else
            {
                zombie = _random.PickAndTake(prefList);
                list.Remove(zombie);
                Logger.InfoS("preset", "Selected a preferred patient 0.");
            }

            var mind = zombie.Data.ContentData()?.Mind;
            if (mind == null)
            {
                Logger.ErrorS("preset", "Failed getting mind for picked patient 0.");
                continue;
            }

            DebugTools.AssertNotNull(mind.OwnedEntity);

            if (mind.OwnedEntity != null)
                _diseaseSystem.TryAddDisease(mind.OwnedEntity.Value, InitialZombieVirusPrototype); //change this once zombie refactor is in.

            if (mind.Session != null)
            {
                var messageWrapper = Loc.GetString("chat-manager-server-wrap-message");

                // I went all the way to ChatManager.cs and all i got was this lousy T-shirt
                _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server, Loc.GetString("zombie-patientzero-role-greeting"),
                   messageWrapper, default, false, mind.Session.ConnectedClient, Color.Plum);
            }
        }
    }

    private void OnPlayersSpawning(RulePlayerSpawningEvent ev)
    {
        if (!Enabled)
            return;
        //these are staying here because i want the code for reference

        // Between 1 and <max op count>: needs at least n players per op.
        /*var numInfected = Math.Max(1,
            (int)Math.Min(
                Math.Floor((double)ev.PlayerPool.Count / _cfg.GetCVar(CCVars.ZombiePlayersPerInfected)), _cfg.GetCVar(CCVars.ZombieMaxInfected)));

        _random.PickAndTake(ev.PlayerPool);
        */

        /*
        var map = "/Maps/infiltrator.yml";

        var aabbs = _stationSystem.Stations.SelectMany(x =>
            Comp<StationDataComponent>(x).Grids.Select(x => _mapManager.GetGridComp(x).Grid.WorldAABB)).ToArray();
        var aabb = aabbs[0];
        for (int i = 1; i < aabbs.Length; i++)
        {
            aabb.Union(aabbs[i]);
        }

        var (_, gridId) = _mapLoader.LoadBlueprint(GameTicker.DefaultMap, map, new MapLoadOptions
        {
            Offset = aabb.Center + MathF.Max(aabb.Height / 2f, aabb.Width / 2f) * 2.5f
        });

        if (!gridId.HasValue)
        {
            Logger.ErrorS("NUKEOPS", $"Gridid was null when loading \"{map}\", aborting.");
            foreach (var session in ops)
            {
                ev.PlayerPool.Add(session);
            }
            return;
        }

        var gridUid = _mapManager.GetGridEuid(gridId.Value);
        
        // TODO: Loot table or something
        var commanderGear = _prototypeManager.Index<StartingGearPrototype>("SyndicateCommanderGearFull");
        var starterGear = _prototypeManager.Index<StartingGearPrototype>("SyndicateOperativeGearFull");
        var medicGear = _prototypeManager.Index<StartingGearPrototype>("SyndicateOperativeMedicFull");

        var spawns = new List<EntityCoordinates>();

        // Forgive me for hardcoding prototypes
        foreach (var (_, meta, xform) in EntityManager.EntityQuery<SpawnPointComponent, MetaDataComponent, TransformComponent>(true))
        {
            if (meta.EntityPrototype?.ID != "SpawnPointNukies" || xform.ParentUid != gridUid) continue;

            spawns.Add(xform.Coordinates);
        }

        if (spawns.Count == 0)
        {
            spawns.Add(EntityManager.GetComponent<TransformComponent>(gridUid).Coordinates);
            Logger.WarningS("nukies", $"Fell back to default spawn for nukies!");
        }
        
        // TODO: This should spawn the nukies in regardless and transfer if possible; rest should go to shot roles.
        for (var i = 0; i < ops.Length; i++)
        {
            string name;
            StartingGearPrototype gear;

            switch (i)
            {
                case 0:
                    name = $"Commander";
                    gear = commanderGear;
                    break;
                case 1:
                    name = $"Operator #{i}";
                    gear = medicGear;
                    break;
                default:
                    name = $"Operator #{i}";
                    gear = starterGear;
                    break;
            }

            var session = ops[i];
            var newMind = new Mind.Mind(session.UserId)
            {
                CharacterName = name
            };
            newMind.ChangeOwningPlayer(session.UserId);

            var mob = EntityManager.SpawnEntity("MobHuman", _random.Pick(spawns));
            EntityManager.GetComponent<MetaDataComponent>(mob).EntityName = name;

            newMind.TransferTo(mob);
            _stationSpawningSystem.EquipStartingGear(mob, gear, null);

            _aliveNukeops.Add(newMind, true);

            GameTicker.PlayerJoinGame(session);
        }*/
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        if (!Enabled)
            return;

        //Uncomment this once im done local testing
        /*
        var minPlayers = _cfg.GetCVar(CCVars.ZombieMinPlayers);
        if (!ev.Forced && ev.Players.Length < minPlayers)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("zombie-not-enough-ready-players", ("readyPlayersCount", ev.Players.Length), ("minimumPlayers", minPlayers)));
            ev.Cancel();
            return;
        }

        if (ev.Players.Length == 0)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("zombie-no-one-ready"));
            ev.Cancel();
            return;
        }
        //*/
    }

    public override void Started()
    {
        _opsWon = false;
    }

    public override void Ended() { }

    /// EVENTS
    /// These are all the functions that handle midround events
    /// that can occur during zombie mode

    private void UnlockNuke()
    {
        _chatManager.DispatchStationAnnouncement(Loc.GetString("zombie-nuke-armed-event", ("code",_nukeCodeSystem.Code)), "Centcomm", default, Color.Crimson);
        foreach (var nuke in EntityManager.EntityQuery<NukeComponent>().ToList())
        {
            if (nuke.DiskSlot.ContainerSlot != null)
                nuke.DiskSlot.ContainerSlot.Insert(Spawn("NukeDisk", Transform(nuke.Owner).Coordinates));
        }
    }
}

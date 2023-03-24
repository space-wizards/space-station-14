using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Objectives.Interfaces;
using Content.Server.Players;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.Traitor;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.Roles;
using Content.Shared.Mobs;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Shared.Mobs.Systems;

namespace Content.Server.GameTicking.Rules;

public sealed class RevolutionaryRuleSystem : GameRuleSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IObjectivesManager _objectivesManager = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawningSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

    private Dictionary<Mind.Mind, bool> _aliveRevoHeads = new();
    private Dictionary<Mind.Mind, bool> _aliveCommandHeads = new();
    private bool _revsWon;
    public override string Prototype => "Revolution";
    private readonly SoundSpecifier _addedSound = new SoundPathSpecifier("/Audio/Misc/tatoralert.ogg");
    private Dictionary<string, string> _revolutionHeadNames = new();

    private const string RevolutionaryHeadPrototypeId = "RevolutionaryHead";


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(GetHead);
    }

    private void GetHead(PlayerSpawnCompleteEvent ev) // ;)
    {
        /// <summary>
        ///     Adds every player that is head of staff to a list
        /// </summary>

        var mind = ev.Player.Data.ContentData()?.Mind;

        if (mind is null)
            return;

        if (mind.CurrentJob is null)
            return;

        // Gets departments of the players current job and checks if one is command
        foreach (var department in mind.CurrentJob.Prototype.Access)
        {
            if (department == "Command")
            {
                if (mind is not null)
                {
                    _aliveCommandHeads.Add(mind, true);
                    return;
                }
            }
        }
        foreach (var accessgroups in mind.CurrentJob.Prototype.AccessGroups)
        {
            if (accessgroups == "AllAccess")
            {
                if (mind is not null)
                {
                     _aliveCommandHeads.Add(mind, true);
                    return;
                }
            }
        }
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        if (!RuleAdded)
            return;

        ev.AddLine(_revsWon ? Loc.GetString("revolutionaries-won") : Loc.GetString("revolutionaries-crew-won"));
        ev.AddLine(Loc.GetString("revolutionaries-list-start"));
        foreach (var player in _revolutionHeadNames)
        {
            ev.AddLine(player.Key + " (" + player.Value + ")");
        }
    }

    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
    {
        if (!RuleAdded)
            return;

        var headsPerRevoHead = _cfg.GetCVar(CCVars.HeadsPerRevolutionaryHead);
        var maxRevoHeads = _cfg.GetCVar(CCVars.MaxRevolutionaryHeads);

        // Adds players that can be antagonist to the list
        var list = new List<IPlayerSession>(ev.Players).Where(x =>
            x.Data.ContentData()?.Mind?.AllRoles.All(role => role is not Job {CanBeAntag: false}) ?? false
        ).ToList();

        var prefList = new List<IPlayerSession>();

        // Adds every player from the list that has Revolutionary Head checked in their preference list to the preflist list
        foreach (var player in list)
        {
            if (!ev.Profiles.ContainsKey(player.UserId))
            {
                continue;
            }
            var profile = ev.Profiles[player.UserId];
            if (profile.AntagPreferences.Contains(RevolutionaryHeadPrototypeId))
            {
                prefList.Add(player);
            }
        }

        var numRevoHeads = MathHelper.Clamp(_aliveCommandHeads.Count / headsPerRevoHead, 1, maxRevoHeads);

        for (var i = 0; i < numRevoHeads; i++)
        {
            IPlayerSession revoHead;
            if(prefList.Count == 0)
            {
                if (list.Count == 0)
                {
                    Logger.InfoS("preset", "Insufficient preffered players ready to fill up with revo heads, stopping the selection.");
                    break;
                }
                revoHead = _random.PickAndTake(list);
                Logger.InfoS("preset", "Insufficient preferred revo heads, picking at random.");
            }
            else
            {
                revoHead = _random.PickAndTake(prefList);
                list.Remove(revoHead);
                Logger.InfoS("preset", "Selected a preferred revo head.");
            }
            var mind = revoHead.ContentData()?.Mind;
            if (mind is null)
            {
                Logger.ErrorS("preset", "Failed getting mind for picked revo head.");
                return;
            }

            var antagPrototype = _prototypeManager.Index<AntagPrototype>(RevolutionaryHeadPrototypeId);
            var revoHeadRole = new TraitorRole(mind, antagPrototype);
            mind.AddRole(revoHeadRole);
            _aliveRevoHeads.Add(mind, true);

            if (mind.OwnedEntity is null)
                return;
            var message = Loc.GetString("revolution-head-role-greeting");
            var messageWrapper = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
            _stationSpawningSystem.EquipStartingGear(mind.OwnedEntity.Value, _prototypeManager.Index<StartingGearPrototype>("RevoHeadGear"), null);

            if (mind.Session == null)
                return;

            var inCharacterName = mind.CharacterName;
            if (inCharacterName != null)
                _revolutionHeadNames.Add(inCharacterName, mind.Session.Name);

            _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server, message,
               messageWrapper, default, false, mind.Session.ConnectedClient, Color.Red);

        }

        SoundSystem.Play(_addedSound.GetSound(), Filter.Empty().AddWhere(s => ((IPlayerSession)s).Data.ContentData()?.Mind?.HasRole<TraitorRole>() ?? false), AudioParams.Default);

    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (!RuleAdded)
            return;
        // Get the first key in the _aliveRevoHeads and if the entity
        // relevant to the event is on said list...
        if (_aliveRevoHeads.TryFirstOrNull(x => x.Key is not null && x.Key.OwnedEntity == ev.Target, out var revohead))
        {
            // ...if so then check if theyre dead
            // and if they are then change their alive bool to false
            _aliveRevoHeads[revohead.Value.Key] = (_mobStateSystem.IsAlive(ev.Target)||_mobStateSystem.IsCritical(ev.Target));

            if (_aliveRevoHeads.Values.All(x => !x))
            {
                _roundEndSystem.EndRound();
            }

        }

        // Same as above ^
        if (_aliveCommandHeads.TryFirstOrNull(x => x.Key is not null && x.Key.OwnedEntity == ev.Target, out var staffhead))
        {
            _aliveCommandHeads[staffhead.Value.Key] = (_mobStateSystem.IsAlive(ev.Target)||_mobStateSystem.IsCritical(ev.Target));

            if (_aliveCommandHeads.Values.All(x => !x))
            {
                _revsWon = true;
                _roundEndSystem.EndRound();
            }
        }
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        if (!RuleAdded)
            return;

        // Check if there are enough players to to start, if not then cancel
        var minPlayers = _cfg.GetCVar(CCVars.RevolutionaryMinPlayers);
        if (!ev.Forced && ev.Players.Length < minPlayers)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("revolutionaries-not-enough-ready-players", ("readyPlayersCount", ev.Players.Length), ("minimumPlayers", minPlayers)));
            ev.Cancel();
            return;
        }

        if (ev.Players.Length == 0)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("revolutionaries-no-one-ready"));
            ev.Cancel();
            return;
        }
    }

    public override void Started()
    {
        _aliveRevoHeads.Clear();
        _aliveCommandHeads.Clear();
        _revolutionHeadNames = new();
        _aliveCommandHeads = new();
        _revsWon = false;
    }

    // This must be implemented even if it does nothing
    public override void Ended() { }
}

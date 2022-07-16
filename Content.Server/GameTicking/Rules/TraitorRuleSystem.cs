using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Objectives.Interfaces;
using Content.Server.Players;
using Content.Server.Roles;
using Content.Server.Traitor;
using Content.Server.Traitor.Uplink;
using Content.Server.Traitor.Uplink.Account;
using Content.Shared.CCVar;
using Content.Shared.Dataset;
using Content.Shared.Roles;
using Content.Shared.Sound;
using Content.Shared.Traitor.Uplink;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Shared.FixedPoint;

namespace Content.Server.GameTicking.Rules;

public sealed class TraitorRuleSystem : GameRuleSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IObjectivesManager _objectivesManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    public override string Prototype => "Traitor";

    private readonly SoundSpecifier _addedSound = new SoundPathSpecifier("/Audio/Misc/tatoralert.ogg");
    private readonly List<TraitorRole> _traitors = new ();

    private const string TraitorPrototypeID = "Traitor";

    public int TotalTraitors => _traitors.Count;

    private int _playersPerTraitor => _cfg.GetCVar(CCVars.TraitorPlayersPerTraitor);

    public HashSet<string> CodeWords = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(HandleLatejoin);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
    }

    public override void Started() {}

    public override void Ended()
    {
        _traitors.Clear();
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        if (!RuleAdded)
            return;

        // If the current preset doesn't explicitly contain the traitor game rule, just carry on and remove self.
        if (_gameTicker.Preset?.Rules.Contains(Prototype) ?? false)
        {
            _gameTicker.EndGameRule(_prototypeManager.Index<GameRulePrototype>(Prototype));
            return;
        }

        var minPlayers = _cfg.GetCVar(CCVars.TraitorMinPlayers);
        if (!ev.Forced && ev.Players.Length < minPlayers)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("traitor-not-enough-ready-players", ("readyPlayersCount", ev.Players.Length), ("minimumPlayers", minPlayers)));
            ev.Cancel();
            return;
        }

        if (ev.Players.Length == 0)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("traitor-no-one-ready"));
            ev.Cancel();
            return;
        }
    }

    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
    {
        if (!RuleAdded)
            return;

        var maxTraitors = _cfg.GetCVar(CCVars.TraitorMaxTraitors);
        var codewordCount = _cfg.GetCVar(CCVars.TraitorCodewordCount);

        var list = new List<IPlayerSession>(ev.Players).Where(x =>
            x.Data.ContentData()?.Mind?.AllRoles.All(role => role is not Job {CanBeAntag: false}) ?? false
        ).ToList();

        var prefList = new List<IPlayerSession>();

        foreach (var player in list)
        {
            if (!ev.Profiles.ContainsKey(player.UserId))
            {
                continue;
            }
            var profile = ev.Profiles[player.UserId];
            if (profile.AntagPreferences.Contains(TraitorPrototypeID))
            {
                prefList.Add(player);
            }
        }

        var numTraitors = MathHelper.Clamp(ev.Players.Length / _playersPerTraitor,
            1, maxTraitors);
        /// Get the initial traitors
        for (var i = 0; i < numTraitors; i++)
        {
            IPlayerSession traitor;
            if(prefList.Count == 0)
            {
                if (list.Count == 0)
                {
                    Logger.InfoS("preset", "Insufficient ready players to fill up with traitors, stopping the selection.");
                    break;
                }
                traitor = _random.PickAndTake(list);
                Logger.InfoS("preset", "Insufficient preferred traitors, picking at random.");
            }
            else
            {
                traitor = _random.PickAndTake(prefList);
                list.Remove(traitor);
                Logger.InfoS("preset", "Selected a preferred traitor.");
            }
            var mind = traitor.Data.ContentData()?.Mind;
            if (mind == null)
            {
                Logger.ErrorS("preset", "Failed getting mind for picked traitor.");
                continue;
            }

            var antagPrototype = _prototypeManager.Index<AntagPrototype>(TraitorPrototypeID);
            var traitorRole = new TraitorRole(mind, antagPrototype);
            mind.AddRole(traitorRole);
            _traitors.Add(traitorRole);
        }

        var adjectives = _prototypeManager.Index<DatasetPrototype>("adjectives").Values;
        var verbs = _prototypeManager.Index<DatasetPrototype>("verbs").Values;

        var codewordPool = adjectives.Concat(verbs).ToList();
        var finalCodewordCount = Math.Min(codewordCount, codewordPool.Count);
        for (var i = 0; i < finalCodewordCount; i++)
        {
            CodeWords.Add(_random.PickAndTake(codewordPool));
        }

        foreach (var traitor in _traitors)
        {
            SetupTraitor(traitor, traitor.Mind);
        }
    }

    private void HandleLatejoin(PlayerSpawnCompleteEvent ev)
    {
        if (!RuleAdded)
            return;
        if (!ev.LateJoin)
            return;
        if (!ev.Profile.AntagPreferences.Contains(TraitorPrototypeID))
            return;
        Logger.Error("Handling latejoin...");

        if (ev.JobId == null || !_prototypeManager.TryIndex<JobPrototype>(ev.JobId, out var job))
            return;

        if (!job.CanBeAntag)
            return;

        // the nth player we adjust our probabilities around
        int target = ((_playersPerTraitor * TotalTraitors) + 1);
        Logger.Error("We have " + TotalTraitors + " traitors, and " + _playersPerTraitor + " players per traitor.  Setting target of " + target);

        float chance = (1f / _playersPerTraitor);
        Logger.Error("Base chance is: " + chance);

        /// If we have too many traitors, divide by how many players below target for next traitor we are.
        Logger.Error("JoinOrder is: " + ev.JoinOrder);
        if (ev.JoinOrder < target)
        {
            chance /= (target - ev.JoinOrder);
        } else // Tick up towards 100% chance.
        {
            chance *= ((ev.JoinOrder + 1) - target);
        }
        if (chance > 1)
            chance = 1;

        Logger.Error("Adjusted chance is: " + chance);

        if (_random.Prob((float) chance))
        {
            Logger.Error("Lucky! Setting up traitor...");
            var mind = ev.Player.Data.ContentData()?.Mind;

            if (mind == null)
            {
                Logger.Error("Player somehow joined with no mind, see entity " + ev.Mob);
                return;
            }

            // Why is this duplicated? Because the round setup thing is adding a bunch at once and needs to avoid collisions
            // so it's in a specific order
            var antagPrototype = _prototypeManager.Index<AntagPrototype>(TraitorPrototypeID);
            var traitorRole = new TraitorRole(mind, antagPrototype);
            mind.AddRole(traitorRole);
            _traitors.Add(traitorRole);

            SetupTraitor(traitorRole, mind);
        }
    }

    private void SetupTraitor(TraitorRole traitor, Mind.Mind mind)
    {
            traitor.GreetTraitor(CodeWords);
            var maxDifficulty = _cfg.GetCVar(CCVars.TraitorMaxDifficulty);
            var maxPicks = _cfg.GetCVar(CCVars.TraitorMaxPicks);
            var startingBalance = _cfg.GetCVar(CCVars.TraitorStartingBalance);

            var uplinkAccount = new UplinkAccount(startingBalance, mind.OwnedEntity!);
            var accounts = EntityManager.EntitySysManager.GetEntitySystem<UplinkAccountsSystem>();
            accounts.AddNewAccount(uplinkAccount);

            if (!EntityManager.EntitySysManager.GetEntitySystem<UplinkSystem>()
                    .AddUplink(mind.OwnedEntity!.Value, uplinkAccount))
                return;

            //give traitors their objectives
            var difficulty = 0f;
            for (var pick = 0; pick < maxPicks && maxDifficulty > difficulty; pick++)
            {
                var objective = _objectivesManager.GetRandomObjective(traitor.Mind);
                if (objective == null) continue;
                if (traitor.Mind.TryAddObjective(objective))
                    difficulty += objective.Difficulty;
            }

            //give traitors their codewords to keep in their character info menu
            traitor.Mind.Briefing = Loc.GetString("traitor-role-codewords", ("codewords", string.Join(", ",CodeWords)));
            SoundSystem.Play(_addedSound.GetSound(), Filter.Entities(mind.OwnedEntity.Value), AudioParams.Default);

    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        if (!RuleAdded)
            return;

        var result = Loc.GetString("traitor-round-end-result", ("traitorCount", _traitors.Count));

        foreach (var traitor in _traitors)
        {
            var name = traitor.Mind.CharacterName;
            traitor.Mind.TryGetSession(out var session);
            var username = session?.Name;

            var objectives = traitor.Mind.AllObjectives.ToArray();
            if (objectives.Length == 0)
            {
                if (username != null)
                {
                    if (name == null)
                        result += "\n" + Loc.GetString("traitor-user-was-a-traitor", ("user", username));
                    else
                        result += "\n" + Loc.GetString("traitor-user-was-a-traitor-named", ("user", username), ("name", name));
                }
                else if (name != null)
                    result += "\n" + Loc.GetString("traitor-was-a-traitor-named", ("name", name));

                continue;
            }

            if (username != null)
            {
                if (name == null)
                    result += "\n" + Loc.GetString("traitor-user-was-a-traitor-with-objectives", ("user", username));
                else
                    result += "\n" + Loc.GetString("traitor-user-was-a-traitor-with-objectives-named", ("user", username), ("name", name));
            }
            else if (name != null)
                result += "\n" + Loc.GetString("traitor-was-a-traitor-with-objectives-named", ("name", name));

            foreach (var objectiveGroup in objectives.GroupBy(o => o.Prototype.Issuer))
            {
                result += "\n" + Loc.GetString($"preset-traitor-objective-issuer-{objectiveGroup.Key}");

                foreach (var objective in objectiveGroup)
                {
                    foreach (var condition in objective.Conditions)
                    {
                        var progress = condition.Progress;
                        if (progress > 0.99f)
                        {
                            result += "\n- " + Loc.GetString(
                                "traitor-objective-condition-success",
                                ("condition", condition.Title),
                                ("markupColor", "green")
                            );
                        }
                        else
                        {
                            result += "\n- " + Loc.GetString(
                                "traitor-objective-condition-fail",
                                ("condition", condition.Title),
                                ("progress", (int) (progress * 100)),
                                ("markupColor", "red")
                            );
                        }
                    }
                }
            }
        }

        ev.AddLine(result);
    }
}

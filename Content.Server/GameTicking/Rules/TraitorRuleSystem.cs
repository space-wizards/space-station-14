using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Objectives.Interfaces;
using Content.Server.Players;
using Content.Server.Roles;
using Content.Server.Traitor;
using Content.Server.Traitor.Uplink;
using Content.Server.MobState;
using Content.Shared.CCVar;
using Content.Shared.Dataset;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Rules;

public sealed class TraitorRuleSystem : GameRuleSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IObjectivesManager _objectivesManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly UplinkSystem _uplink = default!;


    public override string Prototype => "Traitor";

    private readonly SoundSpecifier _addedSound = new SoundPathSpecifier("/Audio/Misc/tatoralert.ogg");
    public List<TraitorRole> Traitors = new();

    private const string TraitorPrototypeID = "Traitor";
    private const string TraitorUplinkPresetId = "StorePresetUplink";

    public int TotalTraitors => Traitors.Count;
    public string[] Codewords = new string[3];

    private int _playersPerTraitor => _cfg.GetCVar(CCVars.TraitorPlayersPerTraitor);
    private int _maxTraitors => _cfg.GetCVar(CCVars.TraitorMaxTraitors);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(HandleLatejoin);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
    }

    public override void Started(){}

    public override void Ended()
    {
        Traitors.Clear();
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        MakeCodewords();
        if (!RuleAdded)
            return;

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

    private void MakeCodewords()
    {

        var codewordCount = _cfg.GetCVar(CCVars.TraitorCodewordCount);
        var adjectives = _prototypeManager.Index<DatasetPrototype>("adjectives").Values;
        var verbs = _prototypeManager.Index<DatasetPrototype>("verbs").Values;
        var codewordPool = adjectives.Concat(verbs).ToList();
        var finalCodewordCount = Math.Min(codewordCount, codewordPool.Count);
        Codewords = new string[finalCodewordCount];
        for (var i = 0; i < finalCodewordCount; i++)
        {
            Codewords[i] = _random.PickAndTake(codewordPool);
        }
    }

    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
    {
        if (!RuleAdded)
            return;

        var numTraitors = MathHelper.Clamp(ev.Players.Length / _playersPerTraitor, 1, _maxTraitors);
        var codewordCount = _cfg.GetCVar(CCVars.TraitorCodewordCount);

        var traitorPool = FindPotentialTraitors(ev);
        var selectedTraitors = PickTraitors(numTraitors, traitorPool);

        foreach (var traitor in selectedTraitors)
            MakeTraitor(traitor);
    }

    public List<IPlayerSession> FindPotentialTraitors(RulePlayerJobsAssignedEvent ev)
    {
        var list = new List<IPlayerSession>(ev.Players).Where(x =>
            x.Data.ContentData()?.Mind?.AllRoles.All(role => role is not Job { CanBeAntag: false }) ?? false
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
        if (prefList.Count == 0)
        {
            Logger.InfoS("preset", "Insufficient preferred traitors, picking at random.");
            prefList = list;
        }
        return prefList;
    }

    public List<IPlayerSession> PickTraitors(int traitorCount, List<IPlayerSession> prefList)
    {
        var results = new List<IPlayerSession>(traitorCount);
        if (prefList.Count == 0)
        {
            Logger.InfoS("preset", "Insufficient ready players to fill up with traitors, stopping the selection.");
            return results;
        }

        for (var i = 0; i < traitorCount; i++)
        {
            results.Add(_random.PickAndTake(prefList));
            Logger.InfoS("preset", "Selected a preferred traitor.");
        }
        return results;
    }

    public bool MakeTraitor(IPlayerSession traitor)
    {
        var mind = traitor.Data.ContentData()?.Mind;
        if (mind == null)
        {
            Logger.ErrorS("preset", "Failed getting mind for picked traitor.");
            return false;
        }

        // creadth: we need to create uplink for the antag.
        // PDA should be in place already
        DebugTools.AssertNotNull(mind.OwnedEntity);

        var startingBalance = _cfg.GetCVar(CCVars.TraitorStartingBalance);

        if (mind.CurrentJob != null)
            startingBalance = Math.Max(startingBalance - mind.CurrentJob.Prototype.AntagAdvantage, 0);

        if (!_uplink.AddUplink(mind.OwnedEntity!.Value, startingBalance))
            return false;

        var antagPrototype = _prototypeManager.Index<AntagPrototype>(TraitorPrototypeID);
        var traitorRole = new TraitorRole(mind, antagPrototype);
        mind.AddRole(traitorRole);
        Traitors.Add(traitorRole);
        traitorRole.GreetTraitor(Codewords);

        var maxDifficulty = _cfg.GetCVar(CCVars.TraitorMaxDifficulty);
        var maxPicks = _cfg.GetCVar(CCVars.TraitorMaxPicks);

        //give traitors their objectives
        var difficulty = 0f;
        for (var pick = 0; pick < maxPicks && maxDifficulty > difficulty; pick++)
        {
            var objective = _objectivesManager.GetRandomObjective(traitorRole.Mind, "TraitorObjectiveGroups");
            if (objective == null) continue;
            if (traitorRole.Mind.TryAddObjective(objective))
                difficulty += objective.Difficulty;
        }

        //give traitors their codewords to keep in their character info menu
        traitorRole.Mind.Briefing = Loc.GetString("traitor-role-codewords", ("codewords", string.Join(", ", Codewords)));

        SoundSystem.Play(_addedSound.GetSound(), Filter.Empty().AddPlayer(traitor), AudioParams.Default);
        return true;
    }

    private void HandleLatejoin(PlayerSpawnCompleteEvent ev)
    {
        if (!RuleAdded)
            return;
        if (TotalTraitors >= _maxTraitors)
            return;
        if (!ev.LateJoin)
            return;
        if (!ev.Profile.AntagPreferences.Contains(TraitorPrototypeID))
            return;


        if (ev.JobId == null || !_prototypeManager.TryIndex<JobPrototype>(ev.JobId, out var job))
            return;

        if (!job.CanBeAntag)
            return;

        // the nth player we adjust our probabilities around
        int target = ((_playersPerTraitor * TotalTraitors) + 1);

        float chance = (1f / _playersPerTraitor);

        /// If we have too many traitors, divide by how many players below target for next traitor we are.
        if (ev.JoinOrder < target)
        {
            chance /= (target - ev.JoinOrder);
        } else // Tick up towards 100% chance.
        {
            chance *= ((ev.JoinOrder + 1) - target);
        }
        if (chance > 1)
            chance = 1;

        // Now that we've calculated our chance, roll and make them a traitor if we roll under.
        // You get one shot.
        if (_random.Prob((float) chance))
        {
            MakeTraitor(ev.Player);
        }
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        if (!RuleAdded)
            return;

        var result = Loc.GetString("traitor-round-end-result", ("traitorCount", Traitors.Count));

        foreach (var traitor in Traitors)
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

    public IEnumerable<Traitor.TraitorRole> GetOtherTraitorsAliveAndConnected(Mind.Mind ourMind)
    {
        var traitors = Traitors;
        List<Traitor.TraitorRole> removeList = new();

        return Traitors // don't want
            .Where(t => t.Mind is not null) // no mind
            .Where(t => t.Mind.OwnedEntity is not null) // no entity
            .Where(t => t.Mind.Session is not null) // player disconnected
            .Where(t => t.Mind != ourMind) // ourselves
            .Where(t => _mobStateSystem.IsAlive((EntityUid) t.Mind.OwnedEntity!)) // dead
            .Where(t => t.Mind.CurrentEntity == t.Mind.OwnedEntity); // not in original body
    }
}

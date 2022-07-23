using System.Collections.Generic;
using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Configurations;
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
    public List<TraitorRole> Traitors = new();

    private const string TraitorPrototypeID = "Traitor";

    public int TotalTraitors => Traitors.Count;
    public string[] Codewords = new string[3];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);
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

        var playersPerTraitor = _cfg.GetCVar(CCVars.TraitorPlayersPerTraitor);
        var maxTraitors = _cfg.GetCVar(CCVars.TraitorMaxTraitors);
        var numTraitors = MathHelper.Clamp(ev.Players.Length / playersPerTraitor, 1, maxTraitors);

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
        // PDA should be in place already, so we just need to
        // initiate uplink account.
        DebugTools.AssertNotNull(mind.OwnedEntity);

        var startingBalance = _cfg.GetCVar(CCVars.TraitorStartingBalance);
        var uplinkAccount = new UplinkAccount(startingBalance, mind.OwnedEntity!);
        var accounts = EntityManager.EntitySysManager.GetEntitySystem<UplinkAccountsSystem>();
        accounts.AddNewAccount(uplinkAccount);

        if (!EntityManager.EntitySysManager.GetEntitySystem<UplinkSystem>().AddUplink(mind.OwnedEntity!.Value, uplinkAccount))
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
            var objective = _objectivesManager.GetRandomObjective(traitorRole.Mind);
            if (objective == null) continue;
            if (traitorRole.Mind.TryAddObjective(objective))
                difficulty += objective.Difficulty;
        }

        //give traitors their codewords to keep in their character info menu
        traitorRole.Mind.Briefing = Loc.GetString("traitor-role-codewords", ("codewords", string.Join(", ", Codewords)));

        SoundSystem.Play(_addedSound.GetSound(), Filter.Empty().AddPlayer(traitor), AudioParams.Default);
        return true;
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
}

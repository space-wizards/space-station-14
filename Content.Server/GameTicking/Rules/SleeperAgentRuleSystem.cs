using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Objectives.Interfaces;
using Content.Server.Players;
using Content.Server.Roles;
using Content.Server.Store.Systems;
using Content.Server.Traitor;
using Content.Server.Traitor.Uplink;
using Content.Shared.CCVar;
using Content.Shared.Dataset;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Server.Radio.Components;
using Content.Shared.Radio;
using Content.Server.Radio.EntitySystems;
using System;

namespace Content.Server.GameTicking.Rules;

public sealed class SleeperAgentRuleSystem : GameRuleSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IObjectivesManager _objectivesManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly UplinkSystem _uplink = default!;
    
    public override string Prototype => "SleeperAgent";
    
    public List<SleeperAgentRole> SleeperAgents = new();
    
    private const string SleeperAgentPrototypeID = "SleeperAgent";
    
    public int TotalAgents => Agents.Count;
    
    private int _playersPerAgent => _cfg.GetCVar(CCVars.AgentPlayersPerAgent);
    private int _maxAgents => _cfg.GetCVar(CCVars.AgentMaxAgents);
    
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
        Agents.Clear();
    }
    
    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
    {
        if (!RuleAdded)
            return;

        var numAgents = MathHelper.Clamp(ev.Players.Length / _playersPerAgent, 1, _maxAgents);
        var codewordCount = _cfg.GetCVar(CCVars.TraitorCodewordCount);
        var phraseCount = _cfg.GetCVar(CCVars.TraitorPhraseCount);

        var agentPool = FindPotentialAgents(ev);
        var selectedAgents = PickTraitors(numAgents, agentPool);

        foreach (var agent in selectedAgent)
            MakeAgent(agent);
    }

    public List<IPlayerSession> FindPotentialAgents(RulePlayerJobsAssignedEvent ev)
    {
        var list = new List<IPlayerSession>(ev.Players).Where(x =>
            x.Data.ContentData()?.Mind?.AllRoles.All
        ).ToList();

        var prefList = new List<IPlayerSession>();

        foreach (var player in list)
        {
            if (!ev.Profiles.ContainsKey(player.UserId))
            {
                continue;
            }
            var profile = ev.Profiles[player.UserId];
            if (profile.AntagPreferences.Contains(AgentPrototypeID))
            {
                prefList.Add(player);
            }
        }
        if (prefList.Count == 0)
        {
            Logger.InfoS("preset", "Insufficient preferred agents, picking at random.");
            prefList = list;
        }
        return prefList;
    }

    public List<IPlayerSession> PickAgents(int agentCount, List<IPlayerSession> prefList)
    {
        var results = new List<IPlayerSession>(agentCount);
        if (prefList.Count == 0)
        {
            Logger.InfoS("preset", "Insufficient ready players to fill up with sleeper agents, stopping the selection.");
            return results;
        }

        for (var i = 0; i < agentCount; i++)
        {
            results.Add(_random.PickAndTake(prefList));
            Logger.InfoS("preset", "Selected a preferred sleeper agent.");
        }
        return results;
    }
    
    bool MakeAgent(IPlayerSession agent)
    {
        var mind = agent.Data.ContentData()?.Mind;
        if (mind == null)
        {
            Logger.ErrorS("preset", "Failed getting mind for chosen sleeper agent.");
            return false;
        }
        mind.AddRole(sleeperagentRole);
        Agents.Add(sleeperagentRole);
    }
       
       // clean up the code after this and make it trigger on phrase or event
  
    protected override void Initialize()
    {
         base.Initialize();

         _sharedInteractionSystem = EntitySystem.Get<SharedInteractionSystem>();
         _triggerSystem = EntitySystem.Get<TriggerSystem>();
    }
 
    bool IListen.CanListen(string message, EntityUid source, RadioChannelPrototype? channelPrototype)
    {
        return _sharedInteractionSystem.InRangeUnobstructed(Owner, source, range: 4);
    }
    void IListen.Listen(string message, EntityUid speaker, RadioChannelPrototype? channel)
        {
            message = message.Trim();

            if (IsPhrase)
            {
                KeyPhrase = "Phrase";
            }
            else if (KeyPhrase != null && message.Contains("Phrase", StringComparison.InvariantCultureIgnoreCase))
            {
                _popupSystem.PopupEntity(Loc.GetString("agent-activation-success"), args.User, Filter.Entities(args.User));
                public bool MakeAgentActivated(IPlayerSession agent)
                {
                    var mind = agent.Data.ContentData()?.Mind;
                    if (mind == null)
                    {
                        Logger.ErrorS("preset", "Failed getting mind for activated sleeper agent.");
                        return false;
                    }

                    // creadth: we need to create uplink for the antag.
                    // PDA should be in place already
                    DebugTools.AssertNotNull(mind.OwnedEntity);

                    var startingBalance = _cfg.GetCVar(CCVars.AgentStartingBalance);

                    if (mind.CurrentJob != null)
                        startingBalance = Math.Max(startingBalance - mind.CurrentJob.Prototype.AntagAdvantage, 0);

                    if (!_uplink.AddUplink(mind.OwnedEntity!.Value, startingBalance))
                        return false;

                    var antagPrototype = _prototypeManager.Index<AntagPrototype>(AgentPrototypeID);
                    var traitorRole = new TraitorRole(mind, antagPrototype);
                    mind.AddRole(activatedagentRole);
                    Agents.Add(activatedagentRole);
                    traitorRole.GreetTraitor(Codewords);

                    var maxDifficulty = _cfg.GetCVar(CCVars.AgentMaxDifficulty);
                    var maxPicks = _cfg.GetCVar(CCVars.AgentMaxPicks);

                    //give traitors their objectives
                    var difficulty = 0f;
                    for (var pick = 0; pick < maxPicks && maxDifficulty > difficulty; pick++)
                    {
                        var objective = _objectivesManager.GetRandomObjective(traitorRole.Mind);
                        if (objective == null) continue;
                        if (activatedagentRole.Mind.TryAddObjective(objective))
                            difficulty += objective.Difficulty;
                    }

                    //give traitors their codewords to keep in their character info menu
                    traitorRole.Mind.Briefing = Loc.GetString("traitor-role-codewords", ("codewords", string.Join(", ", Codewords)));

                    //This gives traitors the agent activation phrase if they have the objective to activate agents.
                    //TODO: Make this automatically fail if the activation event is scheduled (Would just be a freebie)
                    if (ActivateSleeperAgentObjective = true);
                    traitorRole.Mind.Briefing = Loc.GetString("traitor-role-phrases", ("phrases", string.Join(", ", Phrases)));


                    SoundSystem.Play(_addedSound.GetSound(), Filter.Empty().AddPlayer(traitor), AudioParams.Default);
                    return true;
                    {
                    Logger.InfoS("preset", "{$name},{$job}, has been activated as a sleeper agent!");
                    }
                }
            }
 
 void HandleLatejoin(PlayerSpawnCompleteEvent ev)
    {
        if (!RuleAdded)
            return;
        if (TotalTAgents >= _maxAgents)
            return;
        if (!ev.LateJoin)
            return;
        if (!ev.Profile.AntagPreferences.Contains(AgentPrototypeID))
            return;


        if (ev.JobId == null || !_prototypeManager.TryIndex<JobPrototype>(ev.JobId, out var job))
            return;

        // the nth player we adjust our probabilities around
        int target = ((_playersPerAgent * TotalAgents) + 1);

        float chance = (1f / _playersPerAgent);

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
            MakeAgent(ev.Player);
        }
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        if (!RuleAdded)
            return;

        var result = Loc.GetString("agent-round-end-result", ("agentCount", Agents.Count));

        foreach (var traitor in Agents)
        {
            var name = agent.Mind.CharacterName;
            agent.Mind.TryGetSession(out var session);
            var username = session?.Name;

            var objectives = agent.Mind.AllObjectives.ToArray();
            if (objectives.Length == 0)
            {
                if (username != null)
                {
                    if (name == null)
                        result += "\n" + Loc.GetString("agent-user-was-a-sleeper-agent", ("user", username));
                    else
                        result += "\n" + Loc.GetString("agent-user-was-a-sleeper-agent-named", ("user", username), ("name", name));
                }
                else if (name != null)
                    result += "\n" + Loc.GetString("agent-was-a-sleeper-agent-named", ("name", name));

                continue;
            }

            if (username != null)
            {
                if (name == null)
                    result += "\n" + Loc.GetString("agent-user-was-a-activated-sleeper-agent-with-objectives", ("user", username));
                else
                    result += "\n" + Loc.GetString("agent-user-was-a-activated-sleeper-agent-with-objectives-named", ("user", username), ("name", name));
            }
            else if (name != null)
                result += "\n" + Loc.GetString("agent-was-a-activated-sleeper-agent-with-objectives-named", ("name", name));

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


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
    
    public bool MakeAgent(IPlayerSession agent)
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
                _triggerSystem.ToggleRecord(this, Activator, true);
            }
            else if (KeyPhrase != null && message.Contains("Phrase", StringComparison.InvariantCultureIgnoreCase))
            {
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


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

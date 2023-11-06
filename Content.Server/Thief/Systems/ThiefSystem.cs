using Content.Server.Administration.Commands;
using Content.Server.Communications;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Server.Research.Systems;
using Content.Server.Roles;
using Content.Server.GenericAntag;
using Content.Server.Warps;
using Content.Shared.Alert;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Doors.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.Popups;
using Content.Shared.Rounding;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Objectives.Components;

using System.Linq;
using Content.Server.Mind;
using Content.Server.NPC.Systems;
using Content.Server.Objectives;
using Content.Server.PDA.Ringer;
using Content.Server.Shuttles.Components;
using Content.Server.Traitor.Uplink;
using Content.Shared.CCVar;
using Content.Shared.Dataset;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.PDA;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Thief.Systems;
/// <summary>
/// A system that initializes thieves, issues starter items, and monitors goal completion
/// </summary>
public sealed class ThiefSystem : EntitySystem
{
    [Dependency] private readonly RoleSystem _role = default!;

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly UplinkSystem _uplink = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;

    [Dependency] private readonly GameTicker _gameTicker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(HandleLatejoin);
        SubscribeLocalEvent<GameRuleStartedEvent>(GameRuleStarted);
    }

    private void GameRuleStarted(ref GameRuleStartedEvent ev)
    {
        Log.Debug("---------------- Started rule: " + ev.RuleId);
    }

    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev) //Момент после спавна всех игроков
    {
        Log.Error("THIEF IS REAL!");
        foreach(var r in _gameTicker.GetAddedGameRules())
        {
            Log.Error("----- Rule: " + r.ToString());
        }
        //нужно получить текущий режим и записать bool, будет ли работать эта система вообще
        //если он один из подходящего списка, начать добавлять воришек

    }
    private void HandleLatejoin(PlayerSpawnCompleteEvent ev) //Это кстати сработало и при раундстарт подключении. До OnPlayerSpawned
    {
        Log.Error("MAYBE " + ev.Player.Name + " can be a good thief?");
        foreach (var r in _gameTicker.GetAddedGameRules())
        {
            Log.Error("----- Rule: " + r.ToString());
        }
        //нужно получить текущий режим
        //если он один из подходящего списка, начать добавлять воришек
    }

    /// <summary>
    /// Returns a thief's gamerule config data.
    /// If the gamerule was not started then it will be started automatically.
    /// </summary>
    public ThiefRuleComponent? ThiefRule(EntityUid uid, GenericAntagComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return null;

        // mind not added yet so no rule
        if (comp.RuleEntity == null)
            return null;

        return CompOrNull<ThiefRuleComponent>(comp.RuleEntity);
    }
}

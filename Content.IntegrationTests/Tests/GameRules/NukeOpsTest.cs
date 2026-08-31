#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.NUnit.Constraints;
using Content.Server.Body.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Presets;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Components;
using Content.Shared.CCVar;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.NukeOps;
using Content.Shared.Pinpointer;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Station.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.GameRules;

public sealed class NukeOpsTest : GameTest
{
    private static readonly ProtoId<NpcFactionPrototype> SyndicateFaction = "Syndicate";
    private static readonly ProtoId<NpcFactionPrototype> NanotrasenFaction = "NanoTrasen";
    private static readonly ProtoId<AntagPrototype> Nukeops = "Nukeops";
    private static readonly ProtoId<AntagPrototype> NukeopsCommander = "NukeopsCommander";
    private static readonly ProtoId<AntagPrototype> NukeopsMedic = "NukeopsMedic";

    public override PoolSettings PoolSettings => new()
    {
        Dirty = true,
        DummyTicker = false,
        Connected = true,
        InLobby = true
    };

    [SidedDependency(Side.Server)] private MapSystem _sMapSystem = default!;
    [SidedDependency(Side.Server)] private GameTicker _sTicker = default!;
    [SidedDependency(Side.Server)] private MindSystem _sMindSystem = default!;
    [SidedDependency(Side.Server)] private RoleSystem _sRoleSystem = default!;
    [SidedDependency(Side.Server)] private InventorySystem _sInventorySystem = default!;
    [SidedDependency(Side.Server)] private NpcFactionSystem _sFactionSystem = default!;
    [SidedDependency(Side.Server)] private RoundEndSystem _sRoundEndSystem = default!;
    [SidedDependency(Side.Server)] private DamageableSystem _damageSystem = default!;

    /// <summary>
    /// Check that a nuke ops game mode can start without issue. I.e., that the nuke station and such all get loaded.
    /// </summary>
    [Test]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GridFill), true)]
    public async Task TryStopNukeOpsFromConstantlyFailing()
    {
        // Initially in the lobby
        Assert.That(_sTicker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
        Assert.That(Client.AttachedEntity, Is.Null);
        Assert.That(_sTicker.PlayerGameStatuses[Client.User!.Value], Is.EqualTo(PlayerGameStatus.NotReadyToPlay));

        // Add several dummy players
        var dummies = await Server.AddDummySessions(3);
        await RunTicksSync(5);

        // Opt into the nukies role.
        await Pair.SetAntagPreference(NukeopsCommander, true);
        await Pair.SetAntagPreference(NukeopsMedic, true, dummies[1].UserId);

        // Initially, the players have no attached entities
        Assert.That(ServerSession?.AttachedEntity, Is.Null);
        Assert.That(dummies, Has.All.Matches<ICommonSession>(x => x.AttachedEntity == null));

        // There are no grids or maps
        Assert.That(SEntMan.Count<MapComponent>(), Is.Zero);
        Assert.That(SEntMan.Count<MapGridComponent>(), Is.Zero);
        Assert.That(SEntMan.Count<StationMapComponent>(), Is.Zero);
        Assert.That(SEntMan.Count<StationMemberComponent>(), Is.Zero);
        Assert.That(SEntMan.Count<StationCentcommComponent>(), Is.Zero);

        // And no nukie related components
        Assert.That(SEntMan.Count<NukeopsRuleComponent>(), Is.Zero);
        Assert.That(SEntMan.Count<NukeopsRoleComponent>(), Is.Zero);
        Assert.That(SEntMan.Count<NukeOperativeComponent>(), Is.Zero);
        Assert.That(SEntMan.Count<NukeOpsShuttleComponent>(), Is.Zero);
        Assert.That(SEntMan.Count<NukeOperativeSpawnerComponent>(), Is.Zero);

        // Ready up and start nukeops
        _sTicker.ToggleReadyAll(true);
        Assert.That(_sTicker.PlayerGameStatuses.Values, Is.All.EqualTo(PlayerGameStatus.ReadyToPlay));
        await Pair.WaitCommand("forcepreset Nukeops");
        await RunTicksSync(10);

        // Game should have started
        Assert.That(_sTicker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
        Assert.That(_sTicker.PlayerGameStatuses.Values, Is.All.EqualTo(PlayerGameStatus.JoinedGame));
        Assert.That(CEntMan.EntityExists(Client.AttachedEntity));

        var dummyEnts = dummies.Select(x => x.AttachedEntity ?? default).ToArray();
        var player = ServerSession!.AttachedEntity!.Value;
        Assert.That(SEntMan.EntityExists(player));
        Assert.That(dummyEnts, Has.All.Matches<EntityUid>(SEntMan.EntityExists));

        // Maps now exist
        Assert.That(SEntMan.Count<MapComponent>(), Is.GreaterThan(0));
        Assert.That(SEntMan.Count<MapGridComponent>(), Is.GreaterThan(0));
        Assert.That(SEntMan.Count<StationCentcommComponent>(), Is.EqualTo(1));

        // And we now have nukie related components
        Assert.That(SEntMan.Count<NukeopsRuleComponent>(), Is.EqualTo(1));
        Assert.That(SEntMan.Count<NukeopsRoleComponent>(), Is.EqualTo(2));
        Assert.That(SEntMan.Count<NukeOperativeComponent>(), Is.EqualTo(2));
        Assert.That(SEntMan.Count<NukeOpsShuttleComponent>(), Is.EqualTo(1));

        // The player entity should be the nukie commander
        var mind = _sMindSystem.GetMind(player)!.Value;
        Assert.That(player, Has.Comp<NukeOperativeComponent>(Server));
        Assert.That(_sRoleSystem.MindIsAntagonist(mind));
        Assert.That(_sRoleSystem.MindHasRole<NukeopsRoleComponent>(mind));
        Assert.That(_sFactionSystem.IsMember(player, SyndicateFaction), Is.True);
        Assert.That(_sFactionSystem.IsMember(player, NanotrasenFaction), Is.False);
        var roles = _sRoleSystem.MindGetAllRoleInfo(mind);
        var cmdRoles = roles.Where(x => x.Prototype == NukeopsCommander);
        Assert.That(cmdRoles.Count(), Is.EqualTo(1));

        // The second dummy player should be a medic
        var dummyMind = _sMindSystem.GetMind(dummyEnts[1])!.Value;
        Assert.That(dummyEnts[1], Has.Comp<NukeOperativeComponent>(Server));
        Assert.That(_sRoleSystem.MindIsAntagonist(dummyMind));
        Assert.That(_sRoleSystem.MindHasRole<NukeopsRoleComponent>(dummyMind));
        Assert.That(_sFactionSystem.IsMember(dummyEnts[1], SyndicateFaction), Is.True);
        Assert.That(_sFactionSystem.IsMember(dummyEnts[1], NanotrasenFaction), Is.False);
        roles = _sRoleSystem.MindGetAllRoleInfo(dummyMind);
        cmdRoles = roles.Where(x => x.Prototype == NukeopsMedic);
        Assert.That(cmdRoles.Count(), Is.EqualTo(1));

        // The other two players should have just spawned in as normal.
        CheckDummy(0);
        CheckDummy(2);
        void CheckDummy(int i)
        {
            var ent = dummyEnts[i];
            var mindCrew = _sMindSystem.GetMind(ent)!.Value;
            Assert.That(ent, Has.No.Comp<NukeOperativeComponent>(Server));
            Assert.That(_sRoleSystem.MindIsAntagonist(mindCrew), Is.False);
            Assert.That(_sRoleSystem.MindHasRole<NukeopsRoleComponent>(mindCrew), Is.False);
            Assert.That(_sFactionSystem.IsMember(ent, SyndicateFaction), Is.False);
            Assert.That(_sFactionSystem.IsMember(ent, NanotrasenFaction), Is.True);
            var nukeroles = new List<ProtoId<AntagPrototype>> { Nukeops, NukeopsMedic, NukeopsCommander };
            Assert.That(_sRoleSystem.MindGetAllRoleInfo(mindCrew), Has.None.Matches<RoleInfo>(x => nukeroles.Contains(x.Prototype)));
        }

        // The game rule exists, and all the stations/shuttles/maps are properly initialized
        var rule = SEntMan.AllComponents<NukeopsRuleComponent>().Single();
        var ruleComp = rule.Component;
        var gridsRule = SComp<RuleGridsComponent>(rule.Uid);
        foreach (var grid in gridsRule.MapGrids)
        {
            Assert.That(SEntMan.EntityExists(grid));
            Assert.That(grid, Has.Comp<MapGridComponent>(Server));
        }
        Assert.That(SEntMan.EntityExists(ruleComp.TargetStation));
        Assert.That(ruleComp.TargetStation, Has.Comp<StationDataComponent>(Server));

        var nukieShuttle = SEntMan.AllComponents<NukeOpsShuttleComponent>().Single();
        var nukieShuttlEnt = nukieShuttle.Uid;
        Assert.That(SEntMan.EntityExists(nukieShuttlEnt));
        Assert.That(nukieShuttle.Component.AssociatedRule, Is.EqualTo(rule.Uid));

        EntityUid? nukieStationEnt = null;
        foreach (var grid in gridsRule.MapGrids)
        {
            if (SEntMan.HasComponent<StationMemberComponent>(grid))
            {
                nukieStationEnt = grid;
                break;
            }
        }

        Assert.That(SEntMan.EntityExists(nukieStationEnt), Is.False); // its not supposed to be a station!
        Assert.That(_sMapSystem.MapExists(gridsRule.Map));
        var nukieMap = _sMapSystem.GetMap(gridsRule.Map!.Value);

        var targetStation = SComp<StationDataComponent>(ruleComp.TargetStation!.Value);
        var targetGrid = targetStation.Grids.First();
        var targetMap = SComp<TransformComponent>(targetGrid).MapUid!.Value;
        Assert.That(targetMap, Is.Not.EqualTo(nukieMap));

        Assert.That(SComp<TransformComponent>(player).MapUid, Is.EqualTo(nukieMap));
        Assert.That(SComp<TransformComponent>(nukieShuttlEnt).MapUid, Is.EqualTo(nukieMap));

        // The maps are all map-initialized, including the player
        // Yes, this is necessary as this has repeatedly been broken somehow.
        Assert.That(_sMapSystem.IsInitialized(nukieMap));
        Assert.That(_sMapSystem.IsInitialized(targetMap));
        Assert.That(_sMapSystem.IsPaused(nukieMap), Is.False);
        Assert.That(_sMapSystem.IsPaused(targetMap), Is.False);

        EntityLifeStage LifeStage(EntityUid? uid) => SComp<MetaDataComponent>(uid!.Value).EntityLifeStage;
        Assert.That(LifeStage(player), Is.GreaterThan(EntityLifeStage.Initialized));
        Assert.That(LifeStage(nukieMap), Is.GreaterThan(EntityLifeStage.Initialized));
        Assert.That(LifeStage(targetMap), Is.GreaterThan(EntityLifeStage.Initialized));
        Assert.That(LifeStage(nukieShuttlEnt), Is.GreaterThan(EntityLifeStage.Initialized));
        Assert.That(LifeStage(ruleComp.TargetStation), Is.GreaterThan(EntityLifeStage.Initialized));

        // Make sure the player has hands. We've had fucking disarmed nukies before.
        Assert.That(player, Has.Comp<HandsComponent>(Server));
        Assert.That(SComp<HandsComponent>(player).Hands, Is.Not.Empty);

        // While we're at it, lets make sure they aren't naked. I don't know how many inventory slots all mobs will be
        // likely to have in the future. But nukies should probably have at least 3 slots with something in them.
        var enumerator = _sInventorySystem.GetSlotEnumerator(player);
        var total = 0;
        while (enumerator.NextItem(out _))
        {
            total++;
        }
        Assert.That(total, Is.GreaterThan(3));

        // Check the nukie commander passed basic training and figured out how to breathe.
        if (STryComp<RespiratorComponent>(player, out var resp))
        {
            const int totalSeconds = 30;
            var totalTicks = (int)Math.Ceiling(totalSeconds / Server.Timing.TickPeriod.TotalSeconds);
            var increment = 5;
            for (var tick = 0; tick < totalTicks; tick += increment)
            {
                await RunTicksSync(increment);
                Assert.That(resp.SuffocationCycles, Is.LessThanOrEqualTo(resp.SuffocationCycleThreshold));
                Assert.That(_damageSystem.GetTotalDamage(player), Is.EqualTo(FixedPoint2.Zero));
            }
        }

        // Check that the round does not end prematurely when agents are deleted in the outpost
        var nukies = dummyEnts.Where(SEntMan.HasComponent<NukeOperativeComponent>).Append(player).ToArray();
        await Server.WaitAssertion(() =>
        {
            for (var i = 0; i < nukies.Length - 1; i++)
            {
                SDeleteNow(nukies[i]);
                Assert.That(_sRoundEndSystem.IsRoundEndRequested(),
                    Is.False,
                    $"The round ended, but {nukies.Length - i - 1} nukies are still alive!");
            }
            // Delete the last nukie and make sure the round ends.
            SDeleteNow(nukies[^1]);

            Assert.That(_sRoundEndSystem.IsRoundEndRequested(),
                "All nukies were deleted, but the round didn't end!");
        });

        _sTicker.SetGamePreset((GamePresetPrototype?)null);
    }
}

// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Revolutionary.Components;
using Content.Server.Roles;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Roles.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.DeadSpace.Revolutionary;

[TestFixture]
public sealed class RevolutionaryRuleSystemTest
{
    private static readonly EntProtoId Objective = "KillCommandStaffObjective";
    private static readonly ProtoId<NpcFactionPrototype> RevolutionaryFaction = "Revolutionary";

    [Test]
    public async Task ConvertDeconvertReconvertIsIdempotent()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = false,
            Dirty = true,
        });

        var server = pair.Server;
        var entMan = server.EntMan;
        var mindSystem = server.System<MindSystem>();
        var roleSystem = server.System<RoleSystem>();
        var factionSystem = server.System<NpcFactionSystem>();
        var revolutionary = server.System<RevolutionaryRuleSystem>();

        await server.WaitAssertion(() =>
        {
            CreateRule(entMan);
            var head = CreateMindedHuman(entMan, mindSystem, out _);
            var target = CreateMindedHuman(entMan, mindSystem, out var targetMind);
            entMan.EnsureComponent<RevolutionaryComponent>(head);
            entMan.EnsureComponent<HeadRevolutionaryComponent>(head);

            Assert.That(revolutionary.Convert(head, target), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<RevolutionaryComponent>(target), Is.True);
                Assert.That(roleSystem.MindHasRole<RevolutionaryRoleComponent>(targetMind.Owner), Is.True);
                Assert.That(factionSystem.IsMember(target, RevolutionaryFaction), Is.True);
                Assert.That(CountObjectives(entMan, targetMind.Comp), Is.EqualTo(1));
            });

            Assert.That(
                revolutionary.Deconvert(
                    target,
                    stun: false,
                    showPopup: false,
                    showEui: false,
                    "integration test"),
                Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<RevolutionaryComponent>(target), Is.False);
                Assert.That(roleSystem.MindHasRole<RevolutionaryRoleComponent>(targetMind.Owner), Is.False);
                Assert.That(factionSystem.IsMember(target, RevolutionaryFaction), Is.False);
                Assert.That(CountObjectives(entMan, targetMind.Comp), Is.Zero);
            });

            Assert.That(revolutionary.Convert(head, target), Is.True);
            Assert.That(revolutionary.Convert(head, target), Is.False);
            Assert.Multiple(() =>
            {
                Assert.That(roleSystem.MindHasRole<RevolutionaryRoleComponent>(targetMind.Owner), Is.True);
                Assert.That(CountObjectives(entMan, targetMind.Comp), Is.EqualTo(1));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task MindTransferTracksCloneWithoutKeepingOldBody()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = false,
            Dirty = true,
        });

        var server = pair.Server;
        var entMan = server.EntMan;
        var mindSystem = server.System<MindSystem>();
        var revolutionary = server.System<RevolutionaryRuleSystem>();

        await server.WaitAssertion(() =>
        {
            CreateRule(entMan);
            var head = CreateMindedHuman(entMan, mindSystem, out _);
            var original = CreateMindedHuman(entMan, mindSystem, out var revolutionaryMind);
            entMan.EnsureComponent<RevolutionaryComponent>(head);
            entMan.EnsureComponent<HeadRevolutionaryComponent>(head);
            Assert.That(revolutionary.Convert(head, original), Is.True);

            var clone = entMan.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
            entMan.EnsureComponent<RevolutionaryComponent>(clone);
            mindSystem.TransferTo(revolutionaryMind, clone, mind: revolutionaryMind.Comp);

            Assert.That(revolutionary.TryGetRuleState(out var rule), Is.True);
            Assert.That(
                revolutionary.TryGetActiveRevolutionaryBody(revolutionaryMind.Owner, out var activeBody),
                Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(activeBody, Is.EqualTo(clone));
                Assert.That(revolutionary.IsActiveRevolutionaryBody(clone), Is.True);
                Assert.That(revolutionary.IsActiveRevolutionaryBody(original), Is.False);
                Assert.That(rule.Comp.RevolutionaryMinds.Count, Is.EqualTo(2));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task TakingAnotherGhostRoleDoesNotKeepOldRevolutionaryMindActive()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = false,
            Dirty = true,
        });

        var server = pair.Server;
        var entMan = server.EntMan;
        var mindSystem = server.System<MindSystem>();
        var revolutionary = server.System<RevolutionaryRuleSystem>();

        await server.WaitAssertion(() =>
        {
            CreateRule(entMan);
            var head = CreateMindedHuman(entMan, mindSystem, out _);
            var oldBody = CreateMindedHuman(entMan, mindSystem, out var oldMind);
            entMan.EnsureComponent<RevolutionaryComponent>(head);
            entMan.EnsureComponent<HeadRevolutionaryComponent>(head);
            Assert.That(revolutionary.Convert(head, oldBody), Is.True);

            // GhostRoleSystem does this before creating a fresh mind for the selected ghost role.
            mindSystem.WipeMind(oldMind.Owner, oldMind.Comp);
            var guestBody = CreateMindedHuman(entMan, mindSystem, out var guestMind);

            Assert.That(revolutionary.TryGetRuleState(out var rule), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<RevolutionaryComponent>(oldBody), Is.True);
                Assert.That(revolutionary.TryGetActiveRevolutionaryBody(oldMind.Owner, out _), Is.False);
                Assert.That(revolutionary.IsActiveRevolutionaryBody(oldBody), Is.False);
                Assert.That(revolutionary.TryGetActiveRevolutionaryBody(guestMind.Owner, out _), Is.False);
                Assert.That(revolutionary.IsActiveRevolutionaryBody(guestBody), Is.False);
                Assert.That(rule.Comp.RevolutionaryMinds.Count, Is.EqualTo(1));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task CommandMindTransferDoesNotCountCorpseAndCloneTwice()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = false,
            Dirty = true,
        });

        var server = pair.Server;
        var entMan = server.EntMan;
        var mindSystem = server.System<MindSystem>();
        var revolutionary = server.System<RevolutionaryRuleSystem>();

        await server.WaitAssertion(() =>
        {
            CreateRule(entMan);
            var original = CreateMindedHuman(entMan, mindSystem, out var commandMind);
            entMan.EnsureComponent<CommandStaffComponent>(original);

            var clone = entMan.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
            entMan.EnsureComponent<CommandStaffComponent>(clone);
            mindSystem.TransferTo(commandMind, clone, mind: commandMind.Comp);

            Assert.That(revolutionary.TryGetRuleState(out var rule), Is.True);
            Assert.That(
                revolutionary.TryGetTrackedCommandBody(commandMind.Owner, out var activeBody),
                Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(rule.Comp.CommandMinds.Count, Is.EqualTo(1));
                Assert.That(rule.Comp.CommandBodies.Count, Is.EqualTo(1));
                Assert.That(activeBody, Is.EqualTo(clone));
                Assert.That(revolutionary.IsTrackedCommandBody(original), Is.False);
                Assert.That(rule.Comp.CommandDeadFraction, Is.Zero);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DeadCommandRemainsCountedAfterTakingGhostRole()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = false,
            Dirty = true,
        });

        var server = pair.Server;
        var entMan = server.EntMan;
        var mindSystem = server.System<MindSystem>();
        var mobState = server.System<MobStateSystem>();
        var revolutionary = server.System<RevolutionaryRuleSystem>();

        await server.WaitAssertion(() =>
        {
            CreateRule(entMan);
            var command = CreateMindedHuman(entMan, mindSystem, out var commandMind);
            entMan.EnsureComponent<CommandStaffComponent>(command);
            mobState.ChangeMobState(command, MobState.Dead);

            mindSystem.WipeMind(commandMind.Owner, commandMind.Comp);
            var guestBody = CreateMindedHuman(entMan, mindSystem, out var guestMind);

            Assert.That(revolutionary.TryGetRuleState(out var rule), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(rule.Comp.CommandMinds.Count, Is.EqualTo(1));
                Assert.That(rule.Comp.DeadCommandMinds.Count, Is.EqualTo(1));
                Assert.That(rule.Comp.CommandDeadFraction, Is.EqualTo(1f));
                Assert.That(revolutionary.IsTrackedCommandBody(command), Is.True);
                Assert.That(revolutionary.TryGetTrackedCommandBody(guestMind.Owner, out _), Is.False);
                Assert.That(revolutionary.IsTrackedCommandBody(guestBody), Is.False);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task MultipleConversionsShareOneRosterBatchAndSnapshot()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Dirty = true,
            Fresh = true,
        });

        var server = pair.Server;
        var entMan = server.EntMan;
        var mindSystem = server.System<MindSystem>();
        var revolutionary = server.System<RevolutionaryRuleSystem>();
        var map = await pair.CreateTestMap();
        var players = await server.AddDummySessions(3);

        await server.WaitAssertion(() =>
        {
            CreateRule(entMan);
            var bodies = new EntityUid[players.Length];
            for (var i = 0; i < bodies.Length; i++)
            {
                bodies[i] = entMan.SpawnEntity("MobHuman", map.GridCoords);
                mindSystem.ControlMob(players[i].UserId, bodies[i]);
            }

            entMan.EnsureComponent<RevolutionaryComponent>(bodies[0]);
            entMan.EnsureComponent<HeadRevolutionaryComponent>(bodies[0]);
            revolutionary.Update(0f);
            revolutionary.ResetRosterDiagnostics();

            Assert.That(revolutionary.Convert(bodies[0], bodies[1]), Is.True);
            Assert.That(revolutionary.Convert(bodies[0], bodies[2]), Is.True);
            revolutionary.Update(0f);

            Assert.Multiple(() =>
            {
                Assert.That(revolutionary.RosterDeltaBatchCount, Is.EqualTo(1));
                Assert.That(revolutionary.RosterSnapshotBuildCount, Is.EqualTo(1));
                Assert.That(revolutionary.RosterSnapshotBatchCount, Is.EqualTo(1));
                Assert.That(revolutionary.RosterSnapshotSendCount, Is.EqualTo(2));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task MindShieldComponentRemovesAllRevolutionaryState()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = false,
            Dirty = true,
        });

        var server = pair.Server;
        var entMan = server.EntMan;
        var mindSystem = server.System<MindSystem>();
        var roleSystem = server.System<RoleSystem>();
        var factionSystem = server.System<NpcFactionSystem>();
        var revolutionary = server.System<RevolutionaryRuleSystem>();

        await server.WaitAssertion(() =>
        {
            CreateRule(entMan);
            var head = CreateMindedHuman(entMan, mindSystem, out _);
            var target = CreateMindedHuman(entMan, mindSystem, out var targetMind);
            entMan.EnsureComponent<RevolutionaryComponent>(head);
            entMan.EnsureComponent<HeadRevolutionaryComponent>(head);
            Assert.That(revolutionary.Convert(head, target), Is.True);

            entMan.EnsureComponent<MindShieldComponent>(target);

            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<RevolutionaryComponent>(target), Is.False);
                Assert.That(roleSystem.MindHasRole<RevolutionaryRoleComponent>(targetMind.Owner), Is.False);
                Assert.That(factionSystem.IsMember(target, RevolutionaryFaction), Is.False);
                Assert.That(CountObjectives(entMan, targetMind.Comp), Is.Zero);
                Assert.That(revolutionary.IsActiveRevolutionaryBody(target), Is.False);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task LastHeadDefeatsRevolutionOnlyAfterDeath()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = false,
            Dirty = true,
        });

        var server = pair.Server;
        var entMan = server.EntMan;
        var ticker = server.System<GameTicker>();
        var mindSystem = server.System<MindSystem>();
        var mobState = server.System<MobStateSystem>();
        var roleSystem = server.System<RoleSystem>();
        var factionSystem = server.System<NpcFactionSystem>();
        var revolutionary = server.System<RevolutionaryRuleSystem>();

        await server.WaitAssertion(() =>
        {
            var head = CreateMindedHuman(entMan, mindSystem, out var headMind);
            var target = CreateMindedHuman(entMan, mindSystem, out var targetMind);
            entMan.EnsureComponent<RevolutionaryComponent>(head);
            entMan.EnsureComponent<HeadRevolutionaryComponent>(head);

            Assert.That(ticker.StartGameRule("Revolutionary", out var ruleUid), Is.True);
            var rule = entMan.GetComponent<RevolutionaryRuleComponent>(ruleUid);
            Assert.That(revolutionary.Convert(head, target), Is.True);

            mobState.ChangeMobState(head, MobState.PreCritical);
            revolutionary.Update(0f);
            Assert.Multiple(() =>
            {
                Assert.That(mobState.IsPreCritical(head), Is.True);
                Assert.That(rule.DefeatHandled, Is.False);
                Assert.That(ticker.IsGameRuleActive(ruleUid), Is.True);
                Assert.That(rule.HeadRevolutionaryMinds, Does.Contain(headMind.Owner));
                Assert.That(revolutionary.IsActiveRevolutionaryBody(head), Is.True);
                Assert.That(entMan.HasComponent<RevolutionaryComponent>(target), Is.True);
            });

            mobState.ChangeMobState(head, MobState.Critical);
            revolutionary.Update(0f);
            Assert.Multiple(() =>
            {
                Assert.That(mobState.IsCritical(head), Is.True);
                Assert.That(rule.DefeatHandled, Is.False);
                Assert.That(ticker.IsGameRuleActive(ruleUid), Is.True);
                Assert.That(rule.HeadRevolutionaryMinds, Does.Contain(headMind.Owner));
                Assert.That(revolutionary.IsActiveRevolutionaryBody(head), Is.True);
                Assert.That(entMan.HasComponent<RevolutionaryComponent>(target), Is.True);
            });

            mobState.ChangeMobState(head, MobState.Dead);
            revolutionary.Update(0f);
            Assert.Multiple(() =>
            {
                Assert.That(mobState.IsDead(head), Is.True);
                Assert.That(rule.DefeatHandled, Is.True);
                Assert.That(ticker.IsGameRuleActive(ruleUid), Is.False);
                Assert.That(entMan.HasComponent<EndedGameRuleComponent>(ruleUid), Is.True);
                Assert.That(rule.HeadRevolutionaryMinds, Is.Empty);
                Assert.That(entMan.HasComponent<RevolutionaryComponent>(target), Is.False);
                Assert.That(roleSystem.MindHasRole<RevolutionaryRoleComponent>(targetMind.Owner), Is.False);
                Assert.That(factionSystem.IsMember(target, RevolutionaryFaction), Is.False);
                Assert.That(CountObjectives(entMan, targetMind.Comp), Is.Zero);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task AutomaticRoundEndVoteYieldsToActiveRoundAdmin()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = false,
            Dirty = true,
        });

        var server = pair.Server;
        var revolutionary = server.System<RevolutionaryRuleSystem>();
        var adminManager = server.ResolveDependency<IAdminManager>();

        await server.WaitAssertion(() =>
        {
            Assert.That(revolutionary.CanAutomaticallyStartRoundEndVote(), Is.True);
        });

        var admin = await server.AddDummySession();
        await server.WaitAssertion(() => adminManager.PromoteHost(admin));
        await PoolManager.WaitUntil(
            server,
            () => adminManager.ActiveAdmins.Contains(admin),
            maxTicks: 60);

        await server.WaitAssertion(() =>
        {
            Assert.That(revolutionary.CanAutomaticallyStartRoundEndVote(), Is.False);

            adminManager.DeAdmin(admin);
            Assert.That(revolutionary.CanAutomaticallyStartRoundEndVote(), Is.True);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task AdminStatusUsesCachedRosterForEveryHead()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = false,
            Dirty = true,
        });

        var server = pair.Server;
        var entMan = server.EntMan;
        var mindSystem = server.System<MindSystem>();
        var mobStateSystem = server.System<MobStateSystem>();
        var roleSystem = server.System<RoleSystem>();

        await server.WaitAssertion(() =>
        {
            var rule = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
            var ruleComponent = entMan.EnsureComponent<RevolutionaryRuleComponent>(rule);
            entMan.EnsureComponent<GameRuleComponent>(rule);

            var firstHead = CreateMindedHuman(entMan, mindSystem, out var firstMind);
            var secondHead = CreateMindedHuman(entMan, mindSystem, out var secondMind);
            entMan.EnsureComponent<RevolutionaryComponent>(firstHead);
            entMan.EnsureComponent<HeadRevolutionaryComponent>(firstHead);
            entMan.EnsureComponent<RevolutionaryComponent>(secondHead);
            entMan.EnsureComponent<HeadRevolutionaryComponent>(secondHead);
            roleSystem.MindAddRole(firstMind.Owner, "MindRoleRevolutionary");
            roleSystem.MindAddRole(secondMind.Owner, "MindRoleRevolutionary");
            Assert.That(mindSystem.TryAddObjective(firstMind.Owner, firstMind.Comp, Objective), Is.True);
            Assert.That(mindSystem.TryAddObjective(secondMind.Owner, secondMind.Comp, Objective), Is.True);

            var command = CreateMindedHuman(entMan, mindSystem, out _);
            entMan.EnsureComponent<CommandStaffComponent>(command);
            mobStateSystem.ChangeMobState(command, MobState.Dead);

            var status = new CollectGameRuleAdminStatusEvent(rule);
            entMan.EventBus.RaiseLocalEvent(rule, status, true);

            Assert.Multiple(() =>
            {
                Assert.That(ruleComponent.HeadRevolutionaryMinds.Count, Is.EqualTo(2));
                Assert.That(ruleComponent.RevolutionaryMinds.Count, Is.EqualTo(2));
                Assert.That(ruleComponent.CommandDeadFraction, Is.EqualTo(1f));
                Assert.That(status.Sections, Has.Count.EqualTo(1));
                Assert.That(status.Sections[0].Lines, Has.Count.EqualTo(3));
                Assert.That(status.Sections[0].Lines.Count(line => line.Contains("100")), Is.EqualTo(2));
            });
        });

        await pair.CleanReturnAsync();
    }

    private static void CreateRule(IEntityManager entMan)
    {
        var rule = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
        entMan.EnsureComponent<RevolutionaryRuleComponent>(rule);
    }

    private static EntityUid CreateMindedHuman(
        IEntityManager entMan,
        MindSystem mindSystem,
        out Entity<MindComponent> mind)
    {
        var body = entMan.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
        mind = mindSystem.CreateMind(null);
        mindSystem.TransferTo(mind, body, mind: mind.Comp);
        return body;
    }

    private static int CountObjectives(IEntityManager entMan, MindComponent mind)
    {
        return mind.Objectives.Count(objective =>
            entMan.EntityExists(objective) &&
            entMan.GetComponent<MetaDataComponent>(objective).EntityPrototype?.ID == Objective.Id);
    }
}

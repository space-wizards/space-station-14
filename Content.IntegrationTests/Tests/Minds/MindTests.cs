#nullable enable
using System.Linq;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind.Commands;
using Content.Server.Roles;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Players;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Minds;

[TestFixture]
public sealed partial class MindTests
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: MindTestEntityDamageable
  components:
  - type: MindContainer
  - type: Damageable
    damageContainer: Biological
  - type: Body
    prototype: Human
    requiredLegs: 2
  - type: MobState
  - type: MobThresholds
    thresholds:
      0: Alive
      200: Dead
  - type: Destructible
    thresholds:
    - trigger:
        !type:DamageTypeTrigger
        damageType: Blunt
        damage: 400
        behaviors:
        - !type:GibBehavior { }
";

    [Test]
    public async Task TestCreateAndTransferMindToNewEntity()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();

        await server.WaitAssertion(() =>
        {
            var mindSystem = entMan.EntitySysManager.GetEntitySystem<SharedMindSystem>();

            var entity = entMan.SpawnEntity(null, new MapCoordinates());
            var mindComp = entMan.EnsureComponent<MindContainerComponent>(entity);

            var mind = mindSystem.CreateMind(null);

            Assert.That(mind.Comp.UserId, Is.EqualTo(null));

            mindSystem.TransferTo(mind, entity, mind: mind);
            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mind.Owner));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task TestReplaceMind()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();

        await server.WaitAssertion(() =>
        {
            var mindSystem = entMan.EntitySysManager.GetEntitySystem<SharedMindSystem>();

            var entity = entMan.SpawnEntity(null, new MapCoordinates());
            var mindComp = entMan.EnsureComponent<MindContainerComponent>(entity);

            var mindId = mindSystem.CreateMind(null).Owner;
            mindSystem.TransferTo(mindId, entity);
            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mindId));

            var mind2 = mindSystem.CreateMind(null).Owner;
            mindSystem.TransferTo(mind2, entity);
            Assert.Multiple(() =>
            {
                Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mind2));
                var mind = entMan.GetComponent<MindComponent>(mindId);
                Assert.That(mind.OwnedEntity, Is.Not.EqualTo(entity));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task TestEntityDeadWhenGibbed()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();

        EntityUid entity = default!;
        MindContainerComponent mindContainerComp = default!;
        EntityUid mindId = default!;
        var mindSystem = entMan.EntitySysManager.GetEntitySystem<SharedMindSystem>();
        var damageableSystem = entMan.EntitySysManager.GetEntitySystem<DamageableSystem>();

        await server.WaitAssertion(() =>
        {
            entity = entMan.SpawnEntity("MindTestEntityDamageable", new MapCoordinates());
            mindContainerComp = entMan.EnsureComponent<MindContainerComponent>(entity);

            mindId = mindSystem.CreateMind(null);

            mindSystem.TransferTo(mindId, entity);
            Assert.Multiple(() =>
            {
                Assert.That(mindSystem.GetMind(entity, mindContainerComp), Is.EqualTo(mindId));
                var mind = entMan.GetComponent<MindComponent>(mindId);
                Assert.That(!mindSystem.IsCharacterDeadPhysically(mind));
            });
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var damageable = entMan.GetComponent<DamageableComponent>(entity);
            if (!protoMan.TryIndex<DamageTypePrototype>("Blunt", out var prototype))
            {
                return;
            }

            damageableSystem.SetDamage(entity, damageable, new DamageSpecifier(prototype, FixedPoint2.New(401)));
            Assert.That(mindSystem.GetMind(entity, mindContainerComp), Is.EqualTo(mindId));
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var mind = entMan.GetComponent<MindComponent>(mindId);
            Assert.That(mindSystem.IsCharacterDeadPhysically(mind));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task TestMindTransfersToOtherEntity()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();

        await server.WaitAssertion(() =>
        {
            var mindSystem = entMan.EntitySysManager.GetEntitySystem<SharedMindSystem>();

            var entity = entMan.SpawnEntity(null, new MapCoordinates());
            var targetEntity = entMan.SpawnEntity(null, new MapCoordinates());
            var mindComp = entMan.EnsureComponent<MindContainerComponent>(entity);
            entMan.EnsureComponent<MindContainerComponent>(targetEntity);

            var mind = mindSystem.CreateMind(null).Owner;

            mindSystem.TransferTo(mind, entity);

            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mind));

            mindSystem.TransferTo(mind, targetEntity);
            Assert.Multiple(() =>
            {
                Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(null));
                Assert.That(mindSystem.GetMind(targetEntity), Is.EqualTo(mind));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task TestOwningPlayerCanBeChanged()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });
        var server = pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();

        await pair.RunTicksSync(5);
        var mindSystem = entMan.EntitySysManager.GetEntitySystem<SharedMindSystem>();
        var originalMind = GetMind(pair);
        var userId = originalMind.Comp.UserId;

        EntityUid mindId = default!;
        MindComponent mind = default!;
        await server.WaitAssertion(() =>
        {
            var entity = entMan.SpawnEntity(null, new MapCoordinates());
            var mindComp = entMan.EnsureComponent<MindContainerComponent>(entity);
            entMan.DirtyEntity(entity);

            mindId = mindSystem.CreateMind(null);
            mind = entMan.GetComponent<MindComponent>(mindId);
            mindSystem.TransferTo(mindId, entity);
            Assert.Multiple(() =>
            {
                Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mindId));
                Assert.That(mindComp.HasMind);
            });
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            mindSystem.SetUserId(mindId, userId);
            Assert.Multiple(() =>
            {
                Assert.That(mind.UserId, Is.EqualTo(userId));
                Assert.That(originalMind.Comp.UserId, Is.EqualTo(null));
            });

            mindSystem.SetUserId(originalMind.Id, userId);
            Assert.Multiple(() =>
            {
                Assert.That(mind.UserId, Is.EqualTo(null));
                Assert.That(originalMind.Comp.UserId, Is.EqualTo(userId));
            });
        });

        await pair.RunTicksSync(5);

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task TestAddRemoveHasRoles()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();

        await server.WaitAssertion(() =>
        {
            var mindSystem = entMan.EntitySysManager.GetEntitySystem<SharedMindSystem>();
            var roleSystem = entMan.EntitySysManager.GetEntitySystem<SharedRoleSystem>();

            var entity = entMan.SpawnEntity(null, new MapCoordinates());
            var mindComp = entMan.EnsureComponent<MindContainerComponent>(entity);

            var mindId = mindSystem.CreateMind(null).Owner;
            var mind = entMan.EnsureComponent<MindComponent>(mindId);

            Assert.That(mind.UserId, Is.EqualTo(null));

            mindSystem.TransferTo(mindId, entity);
            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mindId));

            Assert.Multiple(() =>
            {
                Assert.That(roleSystem.MindHasRole<TraitorRoleComponent>(mindId), Is.False);
                Assert.That(roleSystem.MindHasRole<JobRoleComponent>(mindId), Is.False);
            });

            var traitorRole = "MindRoleTraitor";

            roleSystem.MindAddRole(mindId, traitorRole);

            Assert.Multiple(() =>
            {
                Assert.That(roleSystem.MindHasRole<TraitorRoleComponent>(mindId));
                Assert.That(roleSystem.MindHasRole<JobRoleComponent>(mindId), Is.False);
            });

            var jobRole = "";

            roleSystem.MindAddJobRole(mindId, jobPrototype:jobRole);

            Assert.Multiple(() =>
            {
                Assert.That(roleSystem.MindHasRole<TraitorRoleComponent>(mindId));
                Assert.That(roleSystem.MindHasRole<JobRoleComponent>(mindId));
            });

            roleSystem.MindRemoveRole<TraitorRoleComponent>(mindId);

            Assert.Multiple(() =>
            {
                Assert.That(roleSystem.MindHasRole<TraitorRoleComponent>(mindId), Is.False);
                Assert.That(roleSystem.MindHasRole<JobRoleComponent>(mindId));
            });

            roleSystem.MindRemoveRole<JobRoleComponent>(mindId);

            Assert.Multiple(() =>
            {
                Assert.That(roleSystem.MindHasRole<TraitorRoleComponent>(mindId), Is.False);
                Assert.That(roleSystem.MindHasRole<JobRoleComponent>(mindId), Is.False);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task TestPlayerCanGhost()
    {
        // Client is needed to spawn session
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, DummyTicker = false });
        var server = pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();

        var mindSystem = entMan.EntitySysManager.GetEntitySystem<SharedMindSystem>();

        EntityUid entity = default!;
        EntityUid mindId = default!;
        MindComponent mind = default!;
        var player = playerMan.Sessions.Single();

        await server.WaitAssertion(() =>
        {
            entity = entMan.SpawnEntity(null, new MapCoordinates());
            var mindComp = entMan.EnsureComponent<MindContainerComponent>(entity);

            mindId = mindSystem.CreateMind(player.UserId, "Mindy McThinker");
            mind = entMan.GetComponent<MindComponent>(mindId);

            Assert.That(mind.UserId, Is.EqualTo(player.UserId));

            mindSystem.TransferTo(mindId, entity);
            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mindId));
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            entMan.DeleteEntity(entity);
        });

        await pair.RunTicksSync(5);

        EntityUid mob = default!;
        EntityUid mobMindId = default!;
        MindComponent mobMind = default!;

        await server.WaitAssertion(() =>
        {
            Assert.That(mind.OwnedEntity, Is.Not.Null);

            mob = entMan.SpawnEntity(null, new MapCoordinates());

            MakeSentientCommand.MakeSentient(mob, entMan);
            mobMindId = mindSystem.CreateMind(player.UserId, "Mindy McThinker the Second");
            mobMind = entMan.GetComponent<MindComponent>(mobMindId);

            mindSystem.SetUserId(mobMindId, player.UserId);
            mindSystem.TransferTo(mobMindId, mob);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var mId = player.ContentData()?.Mind!.Value;
            Assert.That(mId, Is.Not.Null);
            Assert.That(mId, Is.Not.EqualTo(default(EntityUid)));
            var m = entMan.GetComponent<MindComponent>(mId!.Value);
            Assert.Multiple(() =>
            {
                Assert.That(m!.OwnedEntity, Is.EqualTo(mob));
                Assert.That(mId, Is.Not.EqualTo(mindId));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task TestGhostDoesNotInfiniteLoop()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            Dirty = true
        });
        var server = pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();
        var serverConsole = server.ResolveDependency<IServerConsoleHost>();

        //EntityUid entity = default!;
        EntityUid ghostRole = default!;
        EntityUid ghost = default!;
        EntityUid mindId = default!;
        MindComponent mind = default!;
        var player = playerMan.Sessions.Single();

        await server.WaitAssertion(() =>
        {
            // entity = entMan.SpawnEntity(null, new MapCoordinates());
            // var mindComp = entMan.EnsureComponent<MindContainerComponent>(entity);

            // mind = mindSystem.CreateMind(player.UserId, "Mindy McThinker");
            //
            // Assert.That(mind.UserId, Is.EqualTo(player.UserId));
            //
            // mindSystem.TransferTo(mind, entity);
            // Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mind));

            var data = player.ContentData();

            Assert.That(data?.Mind, Is.Not.EqualTo(null));
            mindId = data!.Mind!.Value;
            mind = entMan.GetComponent<MindComponent>(mindId);

            Assert.That(mind.OwnedEntity, Is.Not.Null);

            ghostRole = entMan.SpawnEntity("GhostRoleTestEntity", MapCoordinates.Nullspace);
        });

        await pair.RunTicksSync(20);

        await server.WaitAssertion(() =>
        {
            serverConsole.ExecuteCommand(player, "aghost");
        });

        await pair.RunTicksSync(20);

        await server.WaitAssertion(() =>
        {
            var id = entMan.GetComponent<GhostRoleComponent>(ghostRole).Identifier;
            entMan.EntitySysManager.GetEntitySystem<GhostRoleSystem>().Takeover(player, id);
        });

        await pair.RunTicksSync(20);

        await server.WaitAssertion(() =>
        {
            var data = entMan.GetComponent<MindComponent>(player.ContentData()!.Mind!.Value);
            Assert.That(data.OwnedEntity, Is.EqualTo(ghostRole));

            serverConsole.ExecuteCommand(player, "aghost");
            Assert.That(player.AttachedEntity, Is.Not.Null);
            ghost = player.AttachedEntity!.Value;
        });

        await pair.RunTicksSync(20);

        await server.WaitAssertion(() =>
        {
            Assert.That(player.AttachedEntity, Is.Not.Null);
            Assert.That(player.AttachedEntity!.Value, Is.EqualTo(ghost));
        });

        await pair.CleanReturnAsync();
    }
}

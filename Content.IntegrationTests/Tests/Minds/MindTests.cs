#nullable enable
using System;
using System.Threading.Tasks;
using Content.Server.Mind;
using Content.Server.Mind.Components;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests.Minds;

[TestFixture]
public sealed class MindTests
{
    private const string Prototypes = @"
- type: entity
  id: MindTestEntity
  components:
  - type: Mind

- type: entity
  parent: MindTestEntity
  id: MindTestEntityDamageable
  components:
  - type: Damageable
  - type: MobState
    thresholds:
      0: Alive
      100: Critical
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
    public async Task TestCreateAndTransferMind()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{ NoClient = true });
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();

        await server.WaitAssertion(() =>
        {
            var mindSystem = entMan.EntitySysManager.GetEntitySystem<MindSystem>();

            var entity = entMan.SpawnEntity(null, new MapCoordinates());
            var mindComp = entMan.EnsureComponent<MindComponent>(entity);
            var userId = new NetUserId(Guid.NewGuid());
            var mind = mindSystem.CreateMind(userId);

            Assert.That(mind.UserId, Is.EqualTo(userId));

            mindSystem.TransferTo(mind, entity);
            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mind));
        });

        await pairTracker.CleanReturnAsync();
    }


    [Test]
    public async Task TestEntityDeadWhenGibbed()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{ NoClient = true });
        var server = pairTracker.Pair.Server;

        await server.WaitAssertion(() =>
        {

        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        await pairTracker.CleanReturnAsync();
    }

    public async Task TestGetPlayerFromEntity()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{ NoClient = true });
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();

        await server.WaitAssertion(() =>
        {
            // var playerSession = new PlayerSession();

            var entity = entMan.SpawnEntity(null, new MapCoordinates());

            var mindSys = entMan.EntitySysManager.GetEntitySystem<MindSystem>();

            var mindComp = entMan.GetComponent<MindComponent>(entity);
            // mindComp.Mind?.Session;
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        await pairTracker.CleanReturnAsync();
    }

    [Test]
    public async Task TestMindTransfersToOtherEntity()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{ NoClient = true });
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();

        await server.WaitAssertion(() =>
        {
            var mindSystem = entMan.EntitySysManager.GetEntitySystem<MindSystem>();

            var entity = entMan.SpawnEntity(null, new MapCoordinates());
            var targetEntity = entMan.SpawnEntity(null, new MapCoordinates());
            var mindComp = entMan.EnsureComponent<MindComponent>(entity);
            entMan.EnsureComponent<MindComponent>(targetEntity);

            var mind = mindSystem.CreateMind(new NetUserId(Guid.NewGuid()));

            mindSystem.TransferTo(mind, entity);

            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mind));

            mindSystem.TransferTo(mind, targetEntity);
            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(null));
            Assert.That(mindSystem.GetMind(targetEntity), Is.EqualTo(mind));
        });

        await pairTracker.CleanReturnAsync();
    }

    [Test]
    public async Task TestOwningPlayerCanBeChanged()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{ NoClient = true });
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();

        await server.WaitAssertion(() =>
        {
            var mindSystem = entMan.EntitySysManager.GetEntitySystem<MindSystem>();

            var entity = entMan.SpawnEntity(null, new MapCoordinates());
            var mindComp = entMan.EnsureComponent<MindComponent>(entity);

            var userId = new NetUserId(Guid.NewGuid());
            var mind = mindSystem.CreateMind(userId);

            mindSystem.TransferTo(mind, entity);

            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mind));
            Assert.That(mind.UserId, Is.EqualTo(userId));

            var newUserId = new NetUserId(Guid.NewGuid());
            mindSystem.ChangeOwningPlayer(entity, newUserId, mindComp);
            Assert.That(mind.UserId, Is.EqualTo(newUserId));
        });

        await pairTracker.CleanReturnAsync();
    }
}
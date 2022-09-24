using System.Threading.Tasks;
using Content.Server.Mind;
using Content.Server.Mind.Components;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Minds;

[TestFixture]
public sealed class MindTests
{

    public const string Prototypes = @"
- type: entity
  id: MindTestEntity
  components:
  - type: Damageable
    damageContainer: Biological
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
            var playerSession = new PlayerSession();

            var entity = entMan.SpawnEntity("MindTestEntity", new EntityCoordinates());

            var mindSys = entMan.EntitySysManager.GetEntitySystem<MindSystem>();

            var mindComp = entMan.GetComponent<MindComponent>(entity);
            mindComp.Mind?.Session
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        await pairTracker.CleanReturnAsync();
    }

    public async Task TestMindTransfersToOtherEntity()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{ NoClient = true });
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();

        await server.WaitAssertion(() =>
        {
            var entity = entMan.SpawnEntity("MindTestEntity", new EntityCoordinates());

            var mindSys = entMan.EntitySysManager.GetEntitySystem<MindSystem>();

            var mindComp = entMan.GetComponent<MindComponent>(entity);
            mindComp.Mind?.Session
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        await pairTracker.CleanReturnAsync();
    }
}
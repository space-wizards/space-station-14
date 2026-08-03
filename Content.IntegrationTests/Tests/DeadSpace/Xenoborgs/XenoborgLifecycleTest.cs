// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Linq;
using Content.Server.Destructible;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.DeadSpace.Xenoborgs;

[TestFixture]
public sealed class XenoborgLifecycleTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: XenoborgLifecycleTestCore
  components:
  - type: MothershipCore

- type: entity
  id: XenoborgLifecycleTestUnit
  components:
  - type: MindContainer
  - type: Xenoborg
  - type: BorgTransponder
    name: test xenoborg
    sprite:
      sprite: Mobs/Silicon/chassis.rsi
      state: xenoborg_engi
  - type: InputMover
  - type: TimerTrigger
    delay: 60
";

    [Test]
    public async Task ExistingRoleIsNotDuplicatedAndLastCoreDestroysUnit()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        var entMan = server.EntMan;
        var destructible = server.System<DestructibleSystem>();
        var mindSystem = server.System<SharedMindSystem>();
        var roleSystem = server.System<SharedRoleSystem>();

        await server.WaitAssertion(() =>
        {
            var holder = entMan.SpawnEntity(null, testMap.GridCoords);
            entMan.EnsureComponent<MindContainerComponent>(holder);
            var xenoborg = entMan.SpawnEntity("XenoborgLifecycleTestUnit", testMap.GridCoords);
            var firstCore = entMan.SpawnEntity("XenoborgLifecycleTestCore", testMap.GridCoords);
            var secondCore = entMan.SpawnEntity("XenoborgLifecycleTestCore", testMap.GridCoords);

            var mind = mindSystem.CreateMind(null);
            mindSystem.TransferTo(mind, holder, mind: mind.Comp);
            roleSystem.MindAddRole(mind.Owner, "MindRoleXenoborg", mind.Comp, silent: true);
            Assert.That(CountXenoborgRoles(entMan, mind.Comp), Is.EqualTo(1));

            mindSystem.TransferTo(mind, xenoborg, mind: mind.Comp);
            Assert.That(CountXenoborgRoles(entMan, mind.Comp), Is.EqualTo(1));
            Assert.That(entMan.HasComponent<InputMoverComponent>(xenoborg), Is.True);

            Assert.That(destructible.DestroyEntity(firstCore), Is.True);
            Assert.That(destructible.DestroyEntity(secondCore), Is.True);
            Assert.That(entMan.HasComponent<InputMoverComponent>(xenoborg), Is.False);

            mindSystem.TransferTo(mind, holder, mind: mind.Comp);
            Assert.That(CountXenoborgRoles(entMan, mind.Comp), Is.Zero);
        });

        await pair.CleanReturnAsync();
    }

    private static int CountXenoborgRoles(IEntityManager entMan, MindComponent mind)
    {
        return mind.MindRoleContainer.ContainedEntities.Count(entMan.HasComponent<XenoborgRoleComponent>);
    }
}

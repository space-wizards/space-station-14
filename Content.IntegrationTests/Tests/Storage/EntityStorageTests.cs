using Content.Server.Storage.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Storage;

[TestFixture]
public sealed class EntityStorageTests
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: EntityStorageTest
  name: box
  components:
  - type: EntityStorage
  - type: Damageable
    damageContainer: Inorganic
  - type: Destructible
    thresholds:
    - trigger:
        !type:DamageTrigger
        damage: 10
      behaviors:
      - !type:DoActsBehavior
        acts: [ Destruction ]
";

    [Test]
    public async Task TestContainerDestruction()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        EntityUid box = default;
        EntityUid crowbar = default;
        await server.WaitPost(() => box = server.EntMan.SpawnEntity("EntityStorageTest", map.GridCoords));
        await server.WaitPost(() => crowbar = server.EntMan.SpawnEntity("Crowbar", map.GridCoords));

        // Initially the crowbar is not in a contaienr.
        var sys = server.System<SharedContainerSystem>();
        Assert.That(sys.IsEntityInContainer(crowbar), Is.False);

        // Open then close the storage entity
        var storage = server.System<EntityStorageSystem>();
        await server.WaitPost(() =>
        {
            storage.OpenStorage(box);
            storage.CloseStorage(box);
        });

        // Crowbar is now in the box
        Assert.That(sys.IsEntityInContainer(crowbar));

        // Damage the box
        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Blunt", 100);
        await server.WaitPost(() => server.System<DamageableSystem>().TryChangeDamage(box, damage));

        // Box has been destroyed, contents have been emptied. Destruction uses deffered deletion.
        Assert.That(server.EntMan.IsQueuedForDeletion(box));
        Assert.That(sys.IsEntityInContainer(crowbar), Is.False);

        // Opening and closing the soon-to-be-deleted box should not re-insert the crowbar
        await server.WaitPost(() =>
        {
            storage.OpenStorage(box);
            storage.CloseStorage(box);
        });
        Assert.That(sys.IsEntityInContainer(crowbar), Is.False);

        // Entity gets deleted after a few ticks
        await server.WaitRunTicks(5);
        Assert.That(server.EntMan.Deleted(box));
        Assert.That(server.EntMan.Deleted(crowbar), Is.False);

        await pair.CleanReturnAsync();
    }
}

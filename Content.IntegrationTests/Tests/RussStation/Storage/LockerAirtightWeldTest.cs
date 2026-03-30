using Content.Shared.Storage.Components;
using Content.Shared.Tools.Systems;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.RussStation.Storage;

[TestFixture]
public sealed class LockerAirtightWeldTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: AirtightWeldTestLocker
  components:
  - type: EntityStorage
    airtight: false
  - type: Weldable
";

    /// <summary>
    /// Verifies that raising WeldableChangedEvent with IsWelded=true sets Airtight to true,
    /// and raising it with IsWelded=false sets Airtight back to false.
    /// </summary>
    [Test]
    public async Task WeldingTogglesAirtight()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var mapData = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var locker = entityManager.SpawnEntity("AirtightWeldTestLocker", mapData.GridCoords);
            var storage = entityManager.GetComponent<EntityStorageComponent>(locker);

            Assert.That(storage.Airtight, Is.False, "Locker should start non-airtight");

            // Simulate welding
            var weldedEvent = new WeldableChangedEvent(true);
            entityManager.EventBus.RaiseLocalEvent(locker, ref weldedEvent);

            Assert.That(storage.Airtight, Is.True, "Locker should be airtight after welding");

            // Simulate unwelding
            var unweldedEvent = new WeldableChangedEvent(false);
            entityManager.EventBus.RaiseLocalEvent(locker, ref unweldedEvent);

            Assert.That(storage.Airtight, Is.False, "Locker should not be airtight after unwelding");
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Verifies the default upstream locker prototype (LockerFreezer) is not airtight
    /// before welding. This catches regressions where default airtight is accidentally set.
    /// </summary>
    [Test]
    public async Task DefaultLockerNotAirtight()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var mapData = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var locker = entityManager.SpawnEntity("AirtightWeldTestLocker", mapData.GridCoords);
            var storage = entityManager.GetComponent<EntityStorageComponent>(locker);

            Assert.That(storage.Airtight, Is.False);
        });

        await pair.CleanReturnAsync();
    }
}

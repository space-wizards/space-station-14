using Content.Shared.RussStation.Carrying.Components;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.RussStation.Carrying;

[TestFixture]
[TestOf(typeof(CarrierComponent))]
public sealed class CarryingTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: CarryTestCarrier
  components:
  - type: Carrier
  - type: Carriable
  - type: Hands
  - type: ComplexInteraction
  - type: InputMover
  - type: Physics
    bodyType: KinematicController
  - type: Puller
  - type: StandingState
  - type: MobState
  - type: MobThresholds
    thresholds:
      0: Alive
      100: Dead
  - type: Damageable
    damageContainer: Biological
  - type: Body
    prototype: Human
  - type: Fixtures
    fixtures:
      fix1:
        shape: !type:PhysShapeCircle
          radius: 0.35

- type: entity
  id: CarryTestTarget
  components:
  - type: Carriable
  - type: Physics
    bodyType: KinematicController
  - type: StandingState
  - type: MobState
  - type: MobThresholds
    thresholds:
      0: Alive
      100: Dead
  - type: Damageable
    damageContainer: Biological
  - type: Body
    prototype: Human
  - type: Pullable
  - type: Fixtures
    fixtures:
      fix1:
        shape: !type:PhysShapeCircle
          radius: 0.35
";

    /// <summary>
    /// Verifies CarrierComponent defaults: carrying is null, speed modifiers are set.
    /// </summary>
    [Test]
    public async Task CarrierComponentDefaults()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var mapData = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var carrier = entityManager.SpawnEntity("CarryTestCarrier", mapData.GridCoords);
            var comp = entityManager.GetComponent<CarrierComponent>(carrier);

            Assert.That(comp.Carrying, Is.Null);
            Assert.That(comp.WalkSpeedModifier, Is.EqualTo(0.75f));
            Assert.That(comp.SprintSpeedModifier, Is.EqualTo(0.6f));
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Verifies CarriableComponent defaults: not being carried.
    /// </summary>
    [Test]
    public async Task CarriableComponentDefaults()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var mapData = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var target = entityManager.SpawnEntity("CarryTestTarget", mapData.GridCoords);
            var comp = entityManager.GetComponent<CarriableComponent>(target);

            Assert.That(comp.CarriedBy, Is.Null);
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Verifies that CarrierComponent and CarriableComponent are properly registered
    /// and can be resolved on spawned entities.
    /// </summary>
    [Test]
    public async Task ComponentsRegistered()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var mapData = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var carrier = entityManager.SpawnEntity("CarryTestCarrier", mapData.GridCoords);
            var target = entityManager.SpawnEntity("CarryTestTarget", mapData.GridCoords);

            Assert.That(entityManager.HasComponent<CarrierComponent>(carrier), Is.True);
            Assert.That(entityManager.HasComponent<CarriableComponent>(carrier), Is.True);
            Assert.That(entityManager.HasComponent<CarriableComponent>(target), Is.True);
            Assert.That(entityManager.HasComponent<CarrierComponent>(target), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Verifies that ActiveCarrierComponent and BeingCarriedComponent are NOT present
    /// by default (they are added only during an active carry).
    /// </summary>
    [Test]
    public async Task MarkerComponentsAbsentByDefault()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var mapData = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var carrier = entityManager.SpawnEntity("CarryTestCarrier", mapData.GridCoords);
            var target = entityManager.SpawnEntity("CarryTestTarget", mapData.GridCoords);

            Assert.That(entityManager.HasComponent<ActiveCarrierComponent>(carrier), Is.False);
            Assert.That(entityManager.HasComponent<BeingCarriedComponent>(target), Is.False);
        });

        await pair.CleanReturnAsync();
    }
}

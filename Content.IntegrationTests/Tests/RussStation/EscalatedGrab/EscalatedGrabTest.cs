using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.RussStation.EscalatedGrab;
using Content.Shared.RussStation.EscalatedGrab.Components;
using Content.Shared.RussStation.EscalatedGrab.Systems;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.RussStation.EscalatedGrab;

[TestFixture]
[TestOf(typeof(SharedEscalatedGrabSystem))]
public sealed class EscalatedGrabTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: GrabTestMob
  components:
  - type: Hands
  - type: ComplexInteraction
  - type: InputMover
  - type: Physics
    bodyType: KinematicController
  - type: Puller
  - type: Fixtures
    fixtures:
      fix1:
        shape: !type:PhysShapeCircle
          radius: 0.35

- type: entity
  id: GrabTestTarget
  components:
  - type: Physics
    bodyType: KinematicController
  - type: Pullable
  - type: Fixtures
    fixtures:
      fix1:
        shape: !type:PhysShapeCircle
          radius: 0.35
";

    /// <summary>
    /// Verifies that GetStage returns Pull when no escalation exists.
    /// </summary>
    [Test]
    public async Task DefaultStageIsPull()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var mapData = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var grabSystem = entityManager.System<SharedEscalatedGrabSystem>();

            var puller = entityManager.SpawnEntity("GrabTestMob", mapData.GridCoords);
            var target = entityManager.SpawnEntity("GrabTestTarget", mapData.GridCoords);

            Assert.That(grabSystem.GetStage(puller, target), Is.EqualTo(GrabStage.Pull));
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Verifies that TryEscalate sets the grab stage to Aggressive and adds GrabStateComponent.
    /// </summary>
    [Test]
    public async Task TryEscalateSetsAggressive()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var mapData = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var grabSystem = entityManager.System<SharedEscalatedGrabSystem>();

            var puller = entityManager.SpawnEntity("GrabTestMob", mapData.GridCoords);
            var target = entityManager.SpawnEntity("GrabTestTarget", mapData.GridCoords);

            Assert.That(grabSystem.TryEscalate(puller, target), Is.True);
            Assert.That(entityManager.HasComponent<GrabStateComponent>(puller), Is.True);
            Assert.That(grabSystem.GetStage(puller, target), Is.EqualTo(GrabStage.Aggressive));
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Verifies that TryEscalate on the same target is idempotent.
    /// </summary>
    [Test]
    public async Task TryEscalateSameTargetIdempotent()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var mapData = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var grabSystem = entityManager.System<SharedEscalatedGrabSystem>();

            var puller = entityManager.SpawnEntity("GrabTestMob", mapData.GridCoords);
            var target = entityManager.SpawnEntity("GrabTestTarget", mapData.GridCoords);

            grabSystem.TryEscalate(puller, target);
            Assert.That(grabSystem.TryEscalate(puller, target), Is.True);
            Assert.That(grabSystem.GetStage(puller, target), Is.EqualTo(GrabStage.Aggressive));
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Verifies that HasStage returns true for stages at or below the current stage.
    /// </summary>
    [Test]
    public async Task HasStageChecksMinimum()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var mapData = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var grabSystem = entityManager.System<SharedEscalatedGrabSystem>();

            var puller = entityManager.SpawnEntity("GrabTestMob", mapData.GridCoords);
            var target = entityManager.SpawnEntity("GrabTestTarget", mapData.GridCoords);

            // No escalation: has Pull, not Aggressive
            Assert.That(grabSystem.HasStage(puller, target, GrabStage.Pull), Is.True);
            Assert.That(grabSystem.HasStage(puller, target, GrabStage.Aggressive), Is.False);

            grabSystem.TryEscalate(puller, target);

            // Aggressive: has both Pull and Aggressive
            Assert.That(grabSystem.HasStage(puller, target, GrabStage.Pull), Is.True);
            Assert.That(grabSystem.HasStage(puller, target, GrabStage.Aggressive), Is.True);
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Verifies that ClearEscalation removes the GrabStateComponent and resets stage to Pull.
    /// </summary>
    [Test]
    public async Task ClearEscalationResetsStage()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var mapData = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var grabSystem = entityManager.System<SharedEscalatedGrabSystem>();

            var puller = entityManager.SpawnEntity("GrabTestMob", mapData.GridCoords);
            var target = entityManager.SpawnEntity("GrabTestTarget", mapData.GridCoords);

            grabSystem.TryEscalate(puller, target);
            Assert.That(grabSystem.GetStage(puller, target), Is.EqualTo(GrabStage.Aggressive));

            grabSystem.ClearEscalation(puller);
            Assert.That(entityManager.HasComponent<GrabStateComponent>(puller), Is.False);
            Assert.That(grabSystem.GetStage(puller, target), Is.EqualTo(GrabStage.Pull));
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Verifies that GetStage differentiates between targets for the same puller.
    /// </summary>
    [Test]
    public async Task GetStagePerTarget()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var mapData = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var grabSystem = entityManager.System<SharedEscalatedGrabSystem>();

            var puller = entityManager.SpawnEntity("GrabTestMob", mapData.GridCoords);
            var target1 = entityManager.SpawnEntity("GrabTestTarget", mapData.GridCoords);
            var target2 = entityManager.SpawnEntity("GrabTestTarget", mapData.GridCoords);

            grabSystem.TryEscalate(puller, target1);

            Assert.That(grabSystem.GetStage(puller, target1), Is.EqualTo(GrabStage.Aggressive));
            // Different target should still be Pull (GrabStateComponent tracks one target)
            Assert.That(grabSystem.GetStage(puller, target2), Is.EqualTo(GrabStage.Pull));
        });

        await pair.CleanReturnAsync();
    }
}

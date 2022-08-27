using System.Threading.Tasks;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Chemistry;


// We are adding two non-reactive solutions in these tests
// To ensure volume(A) + volume(B) = volume(A+B)
// reactions can change this assumption
[TestFixture]
[TestOf(typeof(SolutionContainerSystem))]
public sealed class SolutionSystemTests
{
    private const string Prototypes = @"
- type: entity
  id: SolutionTarget
  components:
  - type: SolutionContainerManager
    solutions:
      beaker:
        maxVol: 50
";
    [Test]
    public async Task TryAddTwoNonReactiveReagent()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
        var server = pairTracker.Pair.Server;

        var entityManager = server.ResolveDependency<IEntityManager>();
        var containerSystem = entityManager.EntitySysManager.GetEntitySystem<SolutionContainerSystem>();
        var testMap = await PoolManager.CreateTestMap(pairTracker);
        var coordinates = testMap.GridCoords;

        EntityUid beaker;

        await server.WaitAssertion(() =>
        {
            var oilQuantity = FixedPoint2.New(15);
            var waterQuantity = FixedPoint2.New(10);

            var oilAdded = new Solution("Oil", oilQuantity);
            var originalWater = new Solution("Water", waterQuantity);

            beaker = entityManager.SpawnEntity("SolutionTarget", coordinates);
            Assert.That(containerSystem
                .TryGetSolution(beaker, "beaker", out var solution));

            solution.AddSolution(originalWater);
            Assert.That(containerSystem
                .TryAddSolution(beaker, solution, oilAdded));

            solution.ContainsReagent("Water", out var water);
            solution.ContainsReagent("Oil", out var oil);
            Assert.That(water, Is.EqualTo(waterQuantity));
            Assert.That(oil, Is.EqualTo(oilQuantity));
        });

        await pairTracker.CleanReturnAsync();
    }

    // This test mimics current behavior
    // i.e. if adding too much `TryAddSolution` adding will fail
    [Test]
    public async Task TryAddTooMuchNonReactiveReagent()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
        var server = pairTracker.Pair.Server;

        var testMap = await PoolManager.CreateTestMap(pairTracker);

        var entityManager = server.ResolveDependency<IEntityManager>();
        var containerSystem = entityManager.EntitySysManager.GetEntitySystem<SolutionContainerSystem>();
        var coordinates = testMap.GridCoords;

        EntityUid beaker;

        await server.WaitAssertion(() =>
        {
            var oilQuantity = FixedPoint2.New(1500);
            var waterQuantity = FixedPoint2.New(10);

            var oilAdded = new Solution("Oil", oilQuantity);
            var originalWater = new Solution("Water", waterQuantity);

            beaker = entityManager.SpawnEntity("SolutionTarget", coordinates);
            Assert.That(containerSystem
                .TryGetSolution(beaker, "beaker", out var solution));

            solution.AddSolution(originalWater);
            Assert.That(containerSystem
                .TryAddSolution(beaker, solution, oilAdded), Is.False);

            solution.ContainsReagent("Water", out var water);
            solution.ContainsReagent("Oil", out var oil);
            Assert.That(water, Is.EqualTo(waterQuantity));
            Assert.That(oil, Is.EqualTo(FixedPoint2.Zero));
        });

        await pairTracker.CleanReturnAsync();
    }

    // Unlike TryAddSolution this adds and two solution without then splits leaving only threshold in original
    [Test]
    public async Task TryMixAndOverflowTooMuchReagent()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
        var server = pairTracker.Pair.Server;


        var entityManager = server.ResolveDependency<IEntityManager>();
        var testMap = await PoolManager.CreateTestMap(pairTracker);
        var containerSystem = entityManager.EntitySysManager.GetEntitySystem<SolutionContainerSystem>();
        var coordinates = testMap.GridCoords;

        EntityUid beaker;

        await server.WaitAssertion(() =>
        {
            int ratio = 9;
            int threshold = 20;
            var waterQuantity = FixedPoint2.New(10);
            var oilQuantity = FixedPoint2.New(ratio * waterQuantity.Int());

            var oilAdded = new Solution("Oil", oilQuantity);
            var originalWater = new Solution("Water", waterQuantity);

            beaker = entityManager.SpawnEntity("SolutionTarget", coordinates);
            Assert.That(containerSystem
                .TryGetSolution(beaker, "beaker", out var solution));

            solution.AddSolution(originalWater);
            Assert.That(containerSystem
                .TryMixAndOverflow(beaker, solution, oilAdded, threshold, out var overflowingSolution));

            Assert.That(solution.CurrentVolume, Is.EqualTo(FixedPoint2.New(threshold)));
            solution.ContainsReagent("Water", out var waterMix);
            solution.ContainsReagent("Oil", out var oilMix);
            Assert.That(waterMix, Is.EqualTo(FixedPoint2.New(threshold / (ratio + 1))));
            Assert.That(oilMix, Is.EqualTo(FixedPoint2.New(threshold / (ratio + 1) * ratio)));

            Assert.That(overflowingSolution.CurrentVolume, Is.EqualTo(FixedPoint2.New(80)));
            overflowingSolution.ContainsReagent("Water", out var waterOverflow);
            overflowingSolution.ContainsReagent("Oil", out var oilOverFlow);
            Assert.That(waterOverflow, Is.EqualTo(waterQuantity - waterMix));
            Assert.That(oilOverFlow, Is.EqualTo(oilQuantity - oilMix));
        });

        await pairTracker.CleanReturnAsync();
    }

    // TryMixAndOverflow will fail if Threshold larger than MaxVolume
    [Test]
    public async Task TryMixAndOverflowTooBigOverflow()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
        var server = pairTracker.Pair.Server;

        var entityManager = server.ResolveDependency<IEntityManager>();
        var containerSystem = entityManager.EntitySysManager.GetEntitySystem<SolutionContainerSystem>();
        var testMap = await PoolManager.CreateTestMap(pairTracker);
        var coordinates = testMap.GridCoords;

        EntityUid beaker;

        await server.WaitAssertion(() =>
        {
            int ratio = 9;
            int threshold = 60;
            var waterQuantity = FixedPoint2.New(10);
            var oilQuantity = FixedPoint2.New(ratio * waterQuantity.Int());

            var oilAdded = new Solution("Oil", oilQuantity);
            var originalWater = new Solution("Water", waterQuantity);

            beaker = entityManager.SpawnEntity("SolutionTarget", coordinates);
            Assert.That(containerSystem
                .TryGetSolution(beaker, "beaker", out var solution));

            solution.AddSolution(originalWater);
            Assert.That(containerSystem
                .TryMixAndOverflow(beaker, solution, oilAdded, threshold, out _),
                Is.False);
        });

        await pairTracker.CleanReturnAsync();
    }
}

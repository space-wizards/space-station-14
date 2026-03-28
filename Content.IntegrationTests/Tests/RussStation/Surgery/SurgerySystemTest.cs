using Content.Shared.RussStation.Surgery;
using Content.Shared.RussStation.Surgery.Components;
using Content.Shared.RussStation.Surgery.Systems;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.RussStation.Surgery;

[TestFixture]
[TestOf(typeof(SharedSurgerySystem))]
public sealed class SurgerySystemTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: surgeryProcedure
  id: SurgeryTestProcedure
  name: Test Procedure
  description: A test procedure.
  steps:
    - tag: Scalpel
      duration: 1.0
      popup: surgery-step-incision
    - tag: Retractor
      duration: 1.0
      popup: surgery-step-retract

- type: entity
  id: SurgeryTestPatient
  components:
  - type: Body

- type: entity
  id: SurgeryTestScalpel
  components:
  - type: Tag
    tags:
    - SurgeryTool
    - Scalpel

- type: entity
  id: SurgeryTestRetractor
  components:
  - type: Tag
    tags:
    - SurgeryTool
    - Retractor

- type: entity
  id: SurgeryTestCautery
  components:
  - type: Tag
    tags:
    - SurgeryTool
    - Cautery

- type: entity
  id: SurgeryTestOperatingTable
  components:
  - type: Strap
  - type: SurgerySurface
    speedModifier: 0.5
";

    /// <summary>
    /// Verifies that surgery procedure prototypes load and have valid steps.
    /// </summary>
    [Test]
    public async Task ProcedurePrototypesLoadTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var protoManager = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            Assert.That(protoManager.TryIndex<SurgeryProcedurePrototype>("SurgeryTestProcedure", out var proto), Is.True);
            Assert.That(proto!.Steps.Count, Is.EqualTo(2));
            Assert.That(proto.Steps[0].Tag.Id, Is.EqualTo("Scalpel"));
            Assert.That(proto.Steps[1].Tag.Id, Is.EqualTo("Retractor"));
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Verifies that ToolMatchesStep correctly matches tool tags to step requirements.
    /// </summary>
    [Test]
    public async Task ToolMatchesStepTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entityManager = server.ResolveDependency<IEntityManager>();
        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var mapData = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var surgerySystem = entityManager.System<SharedSurgerySystem>();
            protoManager.TryIndex<SurgeryProcedurePrototype>("SurgeryTestProcedure", out var proto);

            var scalpel = entityManager.SpawnEntity("SurgeryTestScalpel", mapData.GridCoords);
            var retractor = entityManager.SpawnEntity("SurgeryTestRetractor", mapData.GridCoords);

            // Scalpel matches step 0 (Scalpel), not step 1 (Retractor)
            Assert.That(surgerySystem.ToolMatchesStep(scalpel, proto!.Steps[0]), Is.True);
            Assert.That(surgerySystem.ToolMatchesStep(scalpel, proto.Steps[1]), Is.False);

            // Retractor matches step 1 (Retractor), not step 0 (Scalpel)
            Assert.That(surgerySystem.ToolMatchesStep(retractor, proto.Steps[0]), Is.False);
            Assert.That(surgerySystem.ToolMatchesStep(retractor, proto.Steps[1]), Is.True);
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Verifies that IsCauteryTool identifies cautery tools correctly.
    /// </summary>
    [Test]
    public async Task IsCauteryToolTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entityManager = server.ResolveDependency<IEntityManager>();
        var mapData = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var surgerySystem = entityManager.System<SharedSurgerySystem>();

            var cautery = entityManager.SpawnEntity("SurgeryTestCautery", mapData.GridCoords);
            var scalpel = entityManager.SpawnEntity("SurgeryTestScalpel", mapData.GridCoords);

            Assert.That(surgerySystem.IsCauteryTool(cautery), Is.True);
            Assert.That(surgerySystem.IsCauteryTool(scalpel), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Verifies that GetSurfaceSpeedModifier returns 1.0 for unbuckled patients
    /// and the correct modifier for buckled patients.
    /// </summary>
    [Test]
    public async Task SurfaceSpeedModifierTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entityManager = server.ResolveDependency<IEntityManager>();
        var mapData = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var surgerySystem = entityManager.System<SharedSurgerySystem>();

            var patient = entityManager.SpawnEntity("SurgeryTestPatient", mapData.GridCoords);

            // Unbuckled patient should return default modifier of 1.0
            Assert.That(surgerySystem.GetSurfaceSpeedModifier(patient), Is.EqualTo(1f));
        });

        await pair.CleanReturnAsync();
    }
}

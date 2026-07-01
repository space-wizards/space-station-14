using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Tools;

[TestFixture]
[TestOf(typeof(SharedToolSystem))]
public sealed class WelderTests : GameTest
{
    private const string Welder = "TestTinyWelder";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  parent: [SolutionToolWelderMiniEmergency, Welder]
  id: {Welder}
  components:
  - type: Solution
    solution:
      maxVol: 5
      reagents:
      - ReagentId: WeldingFuel
        Quantity: 5
";

    [SidedDependency(Side.Server)] private readonly SharedToolSystem _tool = default!;
    [SidedDependency(Side.Server)] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    [Test]
    public async Task FuelDepletion()
    {
        Entity<WelderComponent> welder = default!;
        Entity<SolutionComponent> fuel = default!;

        await Server.WaitPost(() =>
        {
            var uid = SSpawn(Welder);
            welder = (uid, SComp<WelderComponent>(uid));
            Assume.That(_solutionContainer.TryGetSolution(uid, welder.Comp.FuelSolutionName, out var solutionEnt, out _));
            fuel = solutionEnt!.Value;

            _tool.TurnOn(welder, null);
        });

        await PoolManager.WaitUntil(Server, () => fuel.Comp.Solution.Volume <= 0);

        await Server.WaitPost(() =>
        {
            Assert.That(SComp<ItemToggleComponent>(welder).Activated, Is.False);
        });
    }
}

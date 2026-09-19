using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.NodeContainer;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Atmos;

[TestOf(typeof(AtmosphereSystem))]
public sealed class PipeBurstingTest : AtmosTest
{
    protected override ResPath? TestMapPath => new("Maps/Test/Atmospherics/DeltaPressure/deltapressuretest.yml");

    public const float PressureEpsilon = 10;

    #region Prototypes

    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  parent: GasPipeBase
  id: PipeBurstingTest
  suffix: Straight
  placement:
    mode: AlignAtmosPipeLayers
  components:
  - type: NodeContainer
    nodes:
      pipe:
        !type:PipeNode
        nodeGroupID: Pipe
        pipeDirection: Longitudinal
        maxPressure: 12000
        airBlockedMaxPressureIncreaseFactor: 5
  - type: Sprite
    layers:
    - state: pipeStraight
      map: [ ""enum.PipeVisualLayers.Pipe"" ]
  - type: AtmosPipeLayers
    alternativePrototypes:
      Primary: GasPipeStraight
      Secondary: GasPipeStraightAlt1
      Tertiary: GasPipeStraightAlt2
";

    [Description(
        "Tests that a single pipe doesn't take damage when the pressure differential is not above the bursting threshold.")]
    [TestCase(0, 0)]
    [TestCase(-1, 0)]
    [TestCase(1, 0)]
    [TestCase(0, -1)]
    [TestCase(0, 1)]
    public async Task TestPipeBursting_Single_NoDamageOnVacuum(int x, int y)
    {
        var pipe = await SpawnEntity("PipeBurstingTest",
            new EntityCoordinates(ProcessEnt, new Vector2(x, y)));

        await Server.WaitPost(delegate
        {
            var node = SEntMan.GetComponent<NodeContainerComponent>(pipe);
            var pipeNode = (PipeNode)node.Nodes["pipe"];
            var pipeMix = pipeNode.Air;
            var limit = pipeNode.MaxPressure;
            // raise the pressure of the pipe to just below the bursting threshold
            var targetMoles = IdealGasHelper.SolveMoles(limit - PressureEpsilon, pipeMix.Volume);
            pipeMix.AdjustMoles(Gas.Nitrogen, targetMoles);
        });

        await Server.WaitRunTicks(1);

        await Server.WaitPost(delegate
        {
            SAtmos.RunProcessingFull(ProcessEnt, ProcessEnt.Owner, SAtmos.AtmosTickRate);
            var damageable = Server.System<DamageableSystem>();
            var damagableComp = SEntMan.GetComponent<DamageableComponent>(pipe);
            var damage = damageable.GetPositiveDamage((pipe, damagableComp));
            Assert.That(damage.AnyPositive, Is.False, "Pipe took damage when it shouldn't have.");
        });
    }

    #endregion
}

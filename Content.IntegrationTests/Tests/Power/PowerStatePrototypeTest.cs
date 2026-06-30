using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Power;

[TestFixture, TestOf(typeof(SharedPowerStateSystem))]
public sealed class PowerStatePrototypeTest : GameTest
{
    /// <summary>
    /// Asserts that the <see cref="PowerReceiverComponent"/>'s load is the same
    /// as the idle or working power draw from <see cref="PowerStateComponent"/>,
    /// depending on the current power state.
    /// </summary>
    [Test]
    public async Task AssertApcPowerMatchesPowerState()
    {
        var pair = Pair;
        var server = pair.Server;

        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var entMan = server.ResolveDependency<IEntityManager>();

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(delegate
            {
                foreach (var prototype in protoMan.EnumeratePrototypes<EntityPrototype>()
                             .Where(p => !p.Abstract)
                             .Where(p => !pair.IsTestPrototype(p)))
                {
                    if (!prototype.TryComp<PowerStateComponent>(out var powerStateComp, entMan.ComponentFactory))
                        continue;

                    // LESSON LEARNED:
                    // ENSURE THAT THE COMPONENT YOU ARE TRYING TO GET IS THE SERVER-SIDE VARIANT
                    if (!prototype.TryComp<PowerReceiverComponent>(out var powerReceiverComp, entMan.ComponentFactory))
                    {
                        Assert.Fail(
                            $"Entity prototype '{prototype.ID}' has a PowerStateComponent but is missing the required PowerReceiverComponent.");
                    }

                    var expectedLoad = powerStateComp.IsWorking
                        ? powerStateComp.WorkingPowerDraw
                        : powerStateComp.IdlePowerDraw;

                    Assert.That(powerReceiverComp.DesiredPower,
                        Is.EqualTo(expectedLoad),
                        $"Entity prototype '{prototype.ID}' has mismatched power draw between PowerStateComponent and PowerReceiverComponent.");
                }
            });
        });
    }
}

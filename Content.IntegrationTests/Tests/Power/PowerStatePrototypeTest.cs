using System.Linq;
using Content.Server.Power.Components;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Power;

[TestFixture, TestOf(typeof(SharedPowerStateSystem))]
public sealed class PowerStatePrototypeTest
{
    /// <summary>
    /// Asserts that the <see cref="SharedApcPowerReceiverComponent"/>'s load is the same
    /// as the idle or working power draw from <see cref="PowerStateComponent"/>,
    /// depending on the current power state.
    /// </summary>
    [Test]
    public async Task AssertApcPowerMatchesPowerState()
    {
        await using var pair = await PoolManager.GetServerClient();
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
                    if (!prototype.TryGetComponent<PowerStateComponent>(out var powerStateComp, entMan.ComponentFactory))
                        continue;

                    // LESSON LEARNED:
                    // ENSURE THAT THE COMPONENT YOU ARE TRYING TO GET IS THE SERVER-SIDE VARIANT
                    if (!prototype.TryGetComponent<ApcPowerReceiverComponent>(out var powerReceiverComp, entMan.ComponentFactory))
                    {
                        Assert.Fail(
                            $"Entity prototype '{prototype.ID}' has a PowerStateComponent but is missing the required ApcPowerReceiverComponent.");
                    }

                    var expectedLoad = powerStateComp.IsWorking
                        ? powerStateComp.WorkingPowerDraw
                        : powerStateComp.IdlePowerDraw;

                    Assert.That(powerReceiverComp.Load,
                        Is.EqualTo(expectedLoad),
                        $"Entity prototype '{prototype.ID}' has mismatched power draw between PowerStateComponent and SharedApcPowerReceiverComponent.");
                }
            });
        });

        await pair.CleanReturnAsync();
    }
}

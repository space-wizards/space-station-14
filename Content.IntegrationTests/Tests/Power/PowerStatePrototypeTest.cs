using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Power.Components;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Power;

[TestFixture, TestOf(typeof(SharedPowerStateSystem))]
public sealed class PowerStatePrototypeTest : GameTest
{
    /// <summary>
    /// Asserts that the power load is the same
    /// as the idle or working power draw from <see cref="PowerStateComponent"/>,
    /// depending on the current power state.
    /// </summary>
    [Test]
    [RunOnSide(Side.Server)]
    public async Task AssertApcPowerMatchesPowerState()
    {
        using (Assert.EnterMultipleScope())
        {
            var nonTestEntityPrototypes = SProtoMan.EnumeratePrototypes<EntityPrototype>()
                                                   .Where(p => !p.Abstract)
                                                   .Where(p => !Pair.IsTestPrototype(p));

            foreach (var prototype in nonTestEntityPrototypes)
            {
                if (!prototype.TryComp<PowerStateComponent>(out var powerStateComp, SEntMan.ComponentFactory))
                    continue;

                // LESSON LEARNED:
                // ENSURE THAT THE COMPONENT YOU ARE TRYING TO GET IS THE SERVER-SIDE VARIANT
                if (powerStateComp.EnsureApc && !prototype.HasComp<ApcPowerReceiverComponent>(SEntMan.ComponentFactory))
                {
                    Assert.Fail(
                        $"Entity prototype '{prototype.ID}' has a PowerStateComponent but is missing the required ApcPowerReceiverComponent.");
                }
                
                var expectedLoad = powerStateComp.IsWorking
                    ? powerStateComp.WorkingPowerDraw
                    : powerStateComp.IdlePowerDraw;


                // we have either APC comp and work with APC network, or have PowerConsumer comp and work with higher voltage network
                prototype.TryComp(out ApcPowerReceiverComponent powerReceiverComp, SEntMan.ComponentFactory);
                if (powerReceiverComp != null)
                {
                    Assert.That(powerReceiverComp.Load,
                        Is.EqualTo(expectedLoad),
                        $"Entity prototype '{prototype.ID}' has mismatched power draw between PowerStateComponent and SharedApcPowerReceiverComponent.");
                }
                else
                {
                    Assert.That(prototype.TryComp<PowerConsumerComponent>(out var powerConsumer, SEntMan.ComponentFactory), Is.True);
                    Assert.That(powerConsumer!.DrawRate,
                        Is.EqualTo(expectedLoad),
                        $"Entity prototype '{prototype.ID}' has mismatched power draw between PowerStateComponent and SharedApcPowerReceiverComponent.");
                }
            }
        }
    }
}

using Content.Shared.Damage.Components;

namespace Content.IntegrationTests.Tests.Damageable;

public sealed class StaminaComponentTest
{
    [Test]
    public async Task ValidatePrototypes()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var protos = pair.GetPrototypesWithComponent<StaminaComponent>();

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var (proto, comp) in protos)
                {
                    Assert.That(comp.AnimationThreshold, Is.LessThan(comp.CritThreshold),
                        $"Animation threshold on {proto.ID} must be less than its crit threshold.");
                }
            });
        });

        await pair.CleanReturnAsync();
    }
}

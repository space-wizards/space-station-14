using Content.Server.GameTicking;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class RestartRoundTest
    {
        [Test]
        public async Task Test()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings
            {
                DummyTicker = false,
                Connected = true
            });
            var server = pairTracker.Pair.Server;
            var sysManager = server.ResolveDependency<IEntitySystemManager>();

            await server.WaitPost(() =>
            {
                sysManager.GetEntitySystem<GameTicker>().RestartRound();
            });

            await PoolManager.RunTicksSync(pairTracker.Pair, 10);
            await pairTracker.CleanReturnAsync();
        }
    }
}

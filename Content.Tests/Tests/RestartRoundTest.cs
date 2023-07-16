using Content.Server.GameTicking;
using Robust.Shared.GameObjects;

namespace Content.Tests.Tests
{
    [TestFixture]
    public sealed class RestartRoundTest
    {
        [Test]
        public async Task Test()
        {
            await using var pairTracker = await PoolManager.GetServerClient();
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

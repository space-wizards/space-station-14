using Content.Server.GameTicking;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class RestartRoundTest
    {
        [Test]
        public async Task Test()
        {
            await using var pair = await PoolManager.GetServerClient(new PoolSettings
            {
                DummyTicker = false,
                Connected = true,
                Dirty = true
            });
            var server = pair.Server;
            var sysManager = server.ResolveDependency<IEntitySystemManager>();

            await server.WaitPost(() =>
            {
                sysManager.GetEntitySystem<GameTicker>().RestartRound();
            });

            await pair.RunTicksSync(10);
            await pair.CleanReturnAsync();
        }
    }
}

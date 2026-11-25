using Content.IntegrationTests.Fixtures;
using Content.Server.GameTicking;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class RestartRoundTest : GameTest
    {
        public override PoolSettings PoolSettings => new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            Dirty = true
        };

        [Test]
        public async Task Test()
        {
            var pair = Pair;
            var server = pair.Server;
            var sysManager = server.ResolveDependency<IEntitySystemManager>();

            await server.WaitPost(() =>
            {
                sysManager.GetEntitySystem<GameTicker>().RestartRound();
            });

            await pair.RunUntilSynced();
        }
    }
}

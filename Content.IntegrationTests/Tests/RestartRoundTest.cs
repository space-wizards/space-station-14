using System.Threading.Tasks;
using Content.Server.GameTicking;
using NUnit.Framework;
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
            await using var pairTracker = await PoolManager.GetServerClient();
            var server = pairTracker.Pair.Server;

            await server.WaitPost(() =>
            {
                IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GameTicker>().RestartRound();
            });

            await PoolManager.RunTicksSync(pairTracker.Pair, 10);
            await pairTracker.CleanReturnAsync();
        }
    }
}

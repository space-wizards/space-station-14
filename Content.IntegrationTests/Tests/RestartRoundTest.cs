using System.Threading.Tasks;
using Content.Server.GameTicking;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class RestartRoundTest : ContentIntegrationTest
    {
        [Test]
        public async Task Test()
        {
            var (client, server) = await StartConnectedServerClientPair();

            server.Post(() =>
            {
                IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GameTicker>().RestartRound();
            });

            await RunTicksSync(client, server, 10);
        }
    }
}

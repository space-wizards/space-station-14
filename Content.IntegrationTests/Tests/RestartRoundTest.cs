using Content.Server.Interfaces.GameTicking;
using NUnit.Framework;
using Robust.Shared.IoC;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public class RestartRoundTest : ContentIntegrationTest
    {
        [Test]
        public void Test()
        {
            var (client, server) = StartConnectedServerClientPair();

            server.Post(() =>
            {
                IoCManager.Resolve<IGameTicker>().RestartRound();
            });

            RunTicksSync(client, server, 10);
        }
    }
}

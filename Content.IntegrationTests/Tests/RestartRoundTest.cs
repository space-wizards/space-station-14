using System.Threading.Tasks;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public class RestartRoundTest : ContentIntegrationTest
    {
        [Test]
        public async Task Test()
        {
            var (client, server) = await StartConnectedServerClientPair(serverOptions: new ServerContentIntegrationOption
            {
                CVarOverrides =
                {
                    [CCVars.GameMap.Name] = "saltern",
                    [CCVars.SpawnRadius.Name] = "0.0",
                }
            });

            server.Post(() =>
            {
                IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GameTicker>().RestartRound();
            });

            await RunTicksSync(client, server, 10);
        }
    }
}

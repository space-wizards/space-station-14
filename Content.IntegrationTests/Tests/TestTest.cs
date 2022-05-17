using NUnit.Framework;
using Robust.Shared.IoC;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    internal sealed class TestTest : ContentIntegrationTest
    {
        [Test]
        public async Task Test()
        {
            for (int i = 0; i < 5; i++)
            {
                Debug.WriteLine("Getting new pair");
                var (client, server) = await StartConnectedServerClientPair(
                    new ClientIntegrationOptions
                    {
                        Pool = false
                    },
                    new ServerIntegrationOptions
                    {
                        Pool = false
                    }
                );
                await server.WaitAssertion(() =>
                {
                    var c = client;
                    var manager = IoCManager.Resolve<Robust.Server.Player.IPlayerManager>();
                    var player = manager.ServerSessions.Single();
                    Assert.That(player.Status == Robust.Shared.Enums.SessionStatus.InGame, $"Failed after {i} cycles");
                });
                Debug.WriteLine("Returning pair");
                await OneTimeTearDown();
            }
        }
    }
}

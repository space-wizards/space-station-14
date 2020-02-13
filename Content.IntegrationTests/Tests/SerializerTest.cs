using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Is = NUnit.DeepObjectCompare.Is;

namespace Content.IntegrationTests.Tests
{

    public class SerializerTest : ContentIntegrationTest
    {

        [Test]
        public async Task EntityStatesTest()
        {
            RobustSerializer._traceWriter = Console.Out;
            var client = StartClient();
            var server = StartServer();

            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            // Connect.

            client.SetConnectTarget(server);

            client.Post(() => IoCManager.Resolve<IClientNetManager>().ClientConnect(null, 0, null));

            // Run some ticks for the handshake to complete and such.

                server.RunTicks(1);
                await server.WaitIdleAsync();
                client.RunTicks(1);
                await client.WaitIdleAsync();

            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            // Basic checks to ensure that they're connected and data got replicated.

            var mtx = new ManualResetEventSlim();
            List<EntityState> es = null;

            server.Post(() =>
            {
                var sem = IoCManager.Resolve<IServerEntityManager>();
                es = sem.GetEntityStates(GameTick.Zero);
                mtx.Set();
            });
            mtx.Wait();

            Assert.NotNull(es);

            var serializer = new RobustSerializer();
            
            List<EntityState> roundTrip;
            
            using (var ms = new MemoryStream())
            {
                serializer.Serialize(ms, es);
                ms.Position = 0;
                roundTrip = (List<EntityState>)serializer.Deserialize(ms);
            }
            
            Assert.That(roundTrip, Is.DeepEqualTo(es));

        }

    }

}

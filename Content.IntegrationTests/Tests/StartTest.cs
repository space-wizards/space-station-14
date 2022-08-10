using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.Exceptions;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class StartTest
    {
        /// <summary>
        ///     Test that the server, and client start, and stop.
        /// </summary>
        [Test]
        public async Task TestClientStart()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{Disconnected = true});
            var client = pairTracker.Pair.Client;
            Assert.That(client.IsAlive);
            await client.WaitRunTicks(5);
            Assert.That(client.IsAlive);
            var cRuntimeLog = client.ResolveDependency<IRuntimeLog>();
            Assert.That(cRuntimeLog.ExceptionCount, Is.EqualTo(0), "No exceptions must be logged on client.");
            await client.WaitIdleAsync();
            Assert.That(client.IsAlive);

            var server = pairTracker.Pair.Server;
            Assert.That(server.IsAlive);
            var sRuntimeLog = server.ResolveDependency<IRuntimeLog>();
            Assert.That(sRuntimeLog.ExceptionCount, Is.EqualTo(0), "No exceptions must be logged on server.");
            await server.WaitIdleAsync();
            Assert.That(server.IsAlive);

            await pairTracker.CleanReturnAsync();
        }
    }
}

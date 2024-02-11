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
            await using var pair = await PoolManager.GetServerClient();
            var client = pair.Client;
            Assert.That(client.IsAlive);
            await client.WaitRunTicks(5);
            Assert.That(client.IsAlive);
            var cRuntimeLog = client.ResolveDependency<IRuntimeLog>();
            Assert.That(cRuntimeLog.ExceptionCount, Is.EqualTo(0), "No exceptions must be logged on client.");
            await client.WaitIdleAsync();
            Assert.That(client.IsAlive);

            var server = pair.Server;
            Assert.That(server.IsAlive);
            var sRuntimeLog = server.ResolveDependency<IRuntimeLog>();
            Assert.That(sRuntimeLog.ExceptionCount, Is.EqualTo(0), "No exceptions must be logged on server.");
            await server.WaitIdleAsync();
            Assert.That(server.IsAlive);

            await pair.CleanReturnAsync();
        }
    }
}

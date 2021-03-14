using NUnit.Framework;
using Robust.Shared.Exceptions;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public class StartTest : ContentIntegrationTest
    {
        /// <summary>
        ///     Test that the server starts.
        /// </summary>
        [Test]
        public void TestServerStart()
        {
            var server = StartServer();
            server.RunTicks(5);
            server.WaitIdleAsync();
            Assert.That(server.IsAlive);
            var runtimeLog = server.ResolveDependency<IRuntimeLog>();
            Assert.That(runtimeLog.ExceptionCount, Is.EqualTo(0), "No exceptions must be logged.");
            server.Stop();
            server.WaitIdleAsync();
            Assert.That(!server.IsAlive);
        }

        /// <summary>
        ///     Test that the client starts.
        /// </summary>
        [Test]
        public void TestClientStart()
        {
            var client = StartClient();
            client.WaitIdleAsync();
            Assert.That(client.IsAlive);
            client.RunTicks(5);
            client.WaitIdleAsync();
            Assert.That(client.IsAlive);
            var runtimeLog = client.ResolveDependency<IRuntimeLog>();
            Assert.That(runtimeLog.ExceptionCount, Is.EqualTo(0), "No exceptions must be logged.");
            client.Stop();
            client.WaitIdleAsync();
            Assert.That(!client.IsAlive);
        }
    }
}

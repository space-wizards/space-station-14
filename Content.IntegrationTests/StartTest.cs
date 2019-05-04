using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.Exceptions;
using Robust.Shared.Utility;
using Robust.UnitTesting;

namespace Content.IntegrationTests
{
    [TestFixture]
    public class StartTest : RobustIntegrationTest
    {
        /// <summary>
        ///     Test that the server starts.
        /// </summary>
        [Test]
        public async Task TestServerStart()
        {
            var server = StartServer();
            await server.WaitIdleAsync();
            Assert.That(server.IsAlive);
            server.RunTicks(5);
            await server.WaitIdleAsync();
            Assert.That(server.IsAlive);
            var runtimeLog = server.ResolveDependency<IRuntimeLog>();
            Assert.That(runtimeLog.ExceptionCount, Is.EqualTo(0), "No exceptions must be logged.");
            server.Stop();
            await server.WaitIdleAsync();
            Assert.That(!server.IsAlive);
        }
    }
}

using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Serialization
{
    public class ValidationTest : ContentIntegrationTest
    {
        [Test]
        public async Task Test()
        {
            var server = StartServerDummyTicker();
            await server.WaitIdleAsync();
            var sprotoManager = server.ResolveDependency<IPrototypeManager>();
            var res = sprotoManager.ValidateDirectory(new ResourcePath("/Resources/Prototypes"));
            Assert.That(res.Count, Is.Zero);
        }
    }
}

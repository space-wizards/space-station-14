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
            var server = StartServer();
            await server.WaitIdleAsync();
            var sprotoManager = server.ResolveDependency<IPrototypeManager>();
            await server.WaitAssertion(() =>
                {
                    var res = sprotoManager.ValidateDirectory(new ResourcePath("/Prototypes"));
                    Assert.That(res.Count, Is.Zero);
                }
            );
        }
    }
}

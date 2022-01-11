using Content.Shared;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using System.Linq;
using System.Threading.Tasks;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class IgnoredComponentTest : ContentIntegrationTest
    {
        [Test]
        public async Task CheckIgnoredComponents()
        {
            var (client, server) = await StartConnectedServerClientPair();
            var serverComponents = server.ResolveDependency<IComponentFactory>();
            var ignoredServerNames = ComponentLocations.GetIgnoredServerComponentNames().ToHashSet();
            var clientComponents = client.ResolveDependency<IComponentFactory>();
            var ignoredClientNames = ComponentLocations.GetIgnoredClientComponentNames().ToHashSet();

            foreach(var clientIgnored in ignoredClientNames)
            {
                if (clientComponents.TryGetRegistration(clientIgnored, out _))
                {
                    Assert.Fail($"Component {clientIgnored} was ignored on client, but exists on client");
                }
                if (!serverComponents.TryGetRegistration(clientIgnored, out _))
                {
                    Assert.Fail($"Component {clientIgnored} was ignored on client, but does not exist on server");
                }
            }

            foreach (var serverIgnored in ignoredServerNames)
            {
                if (serverComponents.TryGetRegistration(serverIgnored, out _))
                {
                    Assert.Fail($"Component {serverIgnored} was ignored on server, but exists on server");
                }
                if (!clientComponents.TryGetRegistration(serverIgnored, out _))
                {
                    Assert.Fail($"Component {serverIgnored} was ignored on server, but does not exist on client");
                }
            }
        }
    }
}

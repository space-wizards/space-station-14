using Content.Shared;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using System.Collections.Generic;
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
            var clientComponents = client.ResolveDependency<IComponentFactory>();
            foreach ( (var componentName, var componentLocation) in ComponentLocations.List)
            {
                if ((componentLocation & ComponentLocation.Client) != 0)
                {
                    if (!clientComponents.TryGetRegistration(componentName, out _))
                    {
                        Assert.Fail($"Component {componentName} is set to {componentLocation}, but was not registered on the client");
                    }
                }
                if ((componentLocation & ComponentLocation.Server) != 0)
                {
                    if (!serverComponents.TryGetRegistration(componentName, out _))
                    {
                        Assert.Fail($"Component {componentName} is set to {componentLocation}, but was not registered on the server");
                    }
                }
            }

            foreach (var clientCompType in clientComponents.AllRegisteredTypes)
            {
                var clientCompName = clientComponents.GetRegistration(clientCompType).Name;

                // This handles robust engine adding its own ignores
                if (serverComponents.GetComponentAvailability(clientCompName) == ComponentAvailability.Ignore) continue;

                if (!ComponentLocations.List.Any(c=>c.componentName == clientCompName))
                {
                    Assert.Fail($"Component {clientCompName} was found on the client, but is missing from the component location list");
                }
            }

            foreach (var serverCompType in serverComponents.AllRegisteredTypes)
            {
                var serverCompName = serverComponents.GetRegistration(serverCompType).Name;

                // This handles robust engine adding its own ignores
                if (clientComponents.GetComponentAvailability(serverCompName) == ComponentAvailability.Ignore) continue;

                if (!ComponentLocations.List.Any(c => c.componentName == serverCompName))
                {
                    Assert.Fail($"Component {serverCompName} was found on the server, but is missing from the component location list");
                }
            }
        }
    }
}

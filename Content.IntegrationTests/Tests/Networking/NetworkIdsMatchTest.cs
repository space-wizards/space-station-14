#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;

namespace Content.IntegrationTests.Tests.Networking;

[TestFixture]
public sealed class NetworkIdsMatchTest : GameTest
{
    [Test]
    [Description("Checks that Server and Client have the same networked components registered.")]
    public async Task TestConnect()
    {
        var clientNetComps = CEntMan.ComponentFactory.NetworkedComponents;
        var serverNetComps = SEntMan.ComponentFactory.NetworkedComponents;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(clientNetComps, Is.Not.Null);
            Assert.That(serverNetComps, Is.Not.Null);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(clientNetComps, Has.Count.EqualTo(serverNetComps.Count));

            // Checks that at least Metadata and Transform are registered.
            Assert.That(clientNetComps, Has.Count.GreaterThanOrEqualTo(2));
        }

        var clientNames = clientNetComps.Select(reg => reg.Name);
        var serverNames = serverNetComps.Select(reg => reg.Name);
        Assert.That(clientNames, Is.EqualTo(serverNames));
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.IntegrationTests.Tests.GameObjects.Components
{
    [TestFixture]
    [TestOf(typeof(Client.Entry.IgnoredComponents))]
    [TestOf(typeof(Server.Entry.IgnoredComponents))]
    public class EntityPrototypeComponentsTest : ContentIntegrationTest
    {
        [Test]
        public async Task PrototypesHaveKnownComponents()
        {
            var (client, server) = await StartConnectedServerDummyTickerClientPair();

            await server.WaitIdleAsync();

            var sResourceManager = server.ResolveDependency<IResourceManager>();
            var prototypePath = new ResourcePath("/Prototypes/");
            var paths = sResourceManager.ContentFindFiles(prototypePath)
                .ToList()
                .AsParallel()
                .Where(filePath => filePath.Extension == "yml" &&
                                   !filePath.Filename.StartsWith("."))
                .ToArray();

            var cComponentFactory = client.ResolveDependency<IComponentFactory>();
            var sComponentFactory = server.ResolveDependency<IComponentFactory>();

            var unknownComponentsClient = new List<(string entityId, string component)>();
            var unknownComponentsServer = new List<(string entityId, string component)>();
            var entitiesValidated = 0;
            var componentsValidated = 0;

            foreach (var path in paths)
            {
                var file = sResourceManager.ContentFileRead(path);
                var reader = new StreamReader(file, Encoding.UTF8);

                var yamlStream = new YamlStream();

                Assert.DoesNotThrow(() => yamlStream.Load(reader), "Error while parsing yaml file {0}", path);

                foreach (var document in yamlStream.Documents)
                {
                    var root = (YamlSequenceNode) document.RootNode;

                    foreach (var node in root.Cast<YamlMappingNode>())
                    {
                        var prototypeType = node.GetNode("type").AsString();

                        if (prototypeType != "entity")
                        {
                            continue;
                        }

                        if (!node.TryGetNode<YamlSequenceNode>("components", out var components))
                        {
                            continue;
                        }

                        entitiesValidated++;

                        foreach (var component in components.Cast<YamlMappingNode>())
                        {
                            componentsValidated++;

                            var componentType = component.GetNode("type").AsString();
                            var clientAvailability = cComponentFactory.GetComponentAvailability(componentType);

                            if (clientAvailability == ComponentAvailability.Unknown)
                            {
                                var entityId = node.GetNode("id").AsString();
                                unknownComponentsClient.Add((entityId, componentType));
                            }

                            var serverAvailability = sComponentFactory.GetComponentAvailability(componentType);

                            if (serverAvailability == ComponentAvailability.Unknown)
                            {
                                var entityId = node.GetNode("id").AsString();
                                unknownComponentsServer.Add((entityId, componentType));
                            }
                        }
                    }
                }
            }

            if (unknownComponentsClient.Count + unknownComponentsServer.Count == 0)
            {
                Assert.Pass($"Validated {entitiesValidated} entities with {componentsValidated} components in {paths.Length} files.");
                return;
            }

            var message = new StringBuilder();

            foreach (var unknownComponent in unknownComponentsClient)
            {
                message.Append(
                    $"CLIENT: Unknown component {unknownComponent.component} in prototype {unknownComponent.entityId}\n");
            }

            foreach (var unknownComponent in unknownComponentsServer)
            {
                message.Append(
                    $"SERVER: Unknown component {unknownComponent.component} in prototype {unknownComponent.entityId}\n");
            }

            Assert.Fail(message.ToString());
        }

        [Test]
        public async Task IgnoredComponentsExistInTheCorrectPlaces()
        {
            var (client, server) = await StartConnectedServerClientPair();
            var serverComponents = server.ResolveDependency<IComponentFactory>();
            var ignoredServerNames = Server.Entry.IgnoredComponents.List;
            var clientComponents = client.ResolveDependency<IComponentFactory>();
            var ignoredClientNames = Client.Entry.IgnoredComponents.List;

            var failureMessages = "";
            foreach (var clientIgnored in ignoredClientNames)
            {
                if (clientComponents.TryGetRegistration(clientIgnored, out _))
                {
                    failureMessages = $"{failureMessages}\nComponent {clientIgnored} was ignored on client, but exists on client";
                }
                if (!serverComponents.TryGetRegistration(clientIgnored, out _))
                {
                    failureMessages = $"{failureMessages}\nComponent {clientIgnored} was ignored on client, but does not exist on server";
                }
            }
            foreach (var serverIgnored in ignoredServerNames)
            {
                if (serverComponents.TryGetRegistration(serverIgnored, out _))
                {
                    failureMessages = $"{failureMessages}\nComponent {serverIgnored} was ignored on server, but exists on server";
                }
                if (!clientComponents.TryGetRegistration(serverIgnored, out _))
                {
                    failureMessages = $"{failureMessages}\nComponent {serverIgnored} was ignored on server, but does not exist on client";
                }
            }
            Assert.IsEmpty(failureMessages);
        }
    }
}

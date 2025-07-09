using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.IntegrationTests.Tests.GameObjects.Components
{
    [TestFixture]
    [TestOf(typeof(Server.Entry.IgnoredComponents))]
    public sealed class EntityPrototypeComponentsTest
    {
        [Test]
        public async Task PrototypesHaveKnownComponents()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            var client = pair.Client;

            var sResourceManager = server.ResolveDependency<IResourceManager>();
            var prototypePath = new ResPath("/Prototypes/");
            var paths = sResourceManager.ContentFindFiles(prototypePath)
                .ToList()
                .AsParallel()
                .Where(filePath => filePath.Extension == "yml" &&
                                   !filePath.Filename.StartsWith('.'))
                .ToArray();

            var cComponentFactory = client.ResolveDependency<IComponentFactory>();
            var sComponentFactory = server.ResolveDependency<IComponentFactory>();

            var unknownComponentsClient = new List<(string entityId, string component)>();
            var unknownComponentsServer = new List<(string entityId, string component)>();
            var doubleIgnoredComponents = new List<(string entityId, string component)>();
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
                            var serverAvailability = sComponentFactory.GetComponentAvailability(componentType);

                            var entityId = node.GetNode("id").AsString();

                            if ((clientAvailability, serverAvailability) is
                                (ComponentAvailability.Ignore, ComponentAvailability.Ignore))
                            {
                                doubleIgnoredComponents.Add((entityId, componentType));
                                continue;
                            }

                            // NOTE: currently, the client's component factory is configured to ignore /all/
                            // non-registered components, meaning this case will never succeed. This is here
                            // mainly for future proofing plus any downstreams that were brave enough to not
                            // ignore all unknown components on clientside.
                            if (clientAvailability == ComponentAvailability.Unknown)
                                unknownComponentsClient.Add((entityId, componentType));

                            if (serverAvailability == ComponentAvailability.Unknown)
                                unknownComponentsServer.Add((entityId, componentType));
                        }
                    }
                }
            }

            if (unknownComponentsClient.Count + unknownComponentsServer.Count + doubleIgnoredComponents.Count == 0)
            {
                await pair.CleanReturnAsync();
                Assert.Pass($"Validated {entitiesValidated} entities with {componentsValidated} components in {paths.Length} files.");
                return;
            }

            var message = new StringBuilder();

            foreach (var (entityId, component) in unknownComponentsClient)
            {
                message.Append(
                    $"CLIENT: Unknown component {component} in prototype {entityId}\n");
            }

            foreach (var (entityId, component) in unknownComponentsServer)
            {
                message.Append(
                    $"SERVER: Unknown component {component} in prototype {entityId}\n");
            }

            foreach (var (entityId, component) in doubleIgnoredComponents)
            {
                message.Append(
                    $"Component {component} in prototype {entityId} is ignored by both client and serverV\n");
            }

            Assert.Fail(message.ToString());
        }

        [Test]
        public async Task IgnoredComponentsExistInTheCorrectPlaces()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            var client = pair.Client;
            var serverComponents = server.ResolveDependency<IComponentFactory>();
            var ignoredServerNames = Server.Entry.IgnoredComponents.List;
            var clientComponents = client.ResolveDependency<IComponentFactory>();

            var failureMessages = "";
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
            Assert.That(failureMessages, Is.Empty);
            await pair.CleanReturnAsync();
        }
    }
}

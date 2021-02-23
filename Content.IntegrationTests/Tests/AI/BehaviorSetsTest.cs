using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.AI.Utility;
using Content.Server.AI.Utility.Actions;
using NUnit.Framework;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using YamlDotNet.RepresentationModel;

namespace Content.IntegrationTests.Tests.AI
{
    [TestFixture]
    [TestOf(typeof(BehaviorSetPrototype))]
    public class BehaviorSetsTest : ContentIntegrationTest
    {
        [Test]
        public async Task TestBehaviorSets()
        {
            var options = new ServerIntegrationOptions();
            var server = StartServerDummyTicker(options);
            await server.WaitIdleAsync();

            var protoManager = server.ResolveDependency<IPrototypeManager>();
            var reflectionManager = server.ResolveDependency<IReflectionManager>();

            Dictionary<string, List<string>> behaviorSets = new();

            // Test that all BehaviorSet actions exist.
            await server.WaitAssertion(() =>
            {
                foreach (var proto in protoManager.EnumeratePrototypes<BehaviorSetPrototype>())
                {
                    behaviorSets[proto.ID] = proto.Actions.ToList();

                    foreach (var action in proto.Actions)
                    {
                        if (!reflectionManager.TryLooseGetType(action, out var actionType) ||
                            !typeof(IAiUtility).IsAssignableFrom(actionType))
                        {
                            Assert.Fail($"Action {action} is not valid within BehaviorSet {proto.ID}");
                        }
                    }
                }
            });

            // Test that all BehaviorSets on NPCs exist.
            await server.WaitAssertion(() =>
            {
                foreach (var entity in protoManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (!entity.Components.TryGetValue("UtilityAI", out var npcNode)) continue;
                    var sets = npcNode["behaviorSets"];

                    foreach (var entry in (YamlSequenceNode) sets)
                    {
                        Assert.That(behaviorSets.ContainsKey(entry.ToString()), $"BehaviorSet {entry} in entity {entity.ID} not found");
                    }
                }
            });
        }
    }
}

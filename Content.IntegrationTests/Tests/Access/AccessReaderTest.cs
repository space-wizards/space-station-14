using System.Collections.Generic;
using System.Linq;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Access
{
    [TestFixture]
    [TestOf(typeof(AccessReaderComponent))]
    public sealed class AccessReaderTest
    {
        [Test]
        public async Task TestProtoTags()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var protoManager = server.ResolveDependency<IPrototypeManager>();
            var accessName = server.ResolveDependency<IComponentFactory>().GetComponentName(typeof(AccessReaderComponent));

            await server.WaitAssertion(() =>
            {
                foreach (var ent in protoManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (!ent.Components.TryGetComponent(accessName, out var access))
                        continue;

                    var reader = (AccessReaderComponent) access;
                    var allTags = reader.AccessLists.SelectMany(c => c).Union(reader.DenyTags);

                    foreach (var level in allTags)
                    {
                        Assert.That(protoManager.HasIndex<AccessLevelPrototype>(level), $"Invalid access level: {level} found on {ent}");
                    }
                }
            });

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task TestTags()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            var entityManager = server.ResolveDependency<IEntityManager>();


            await server.WaitAssertion(() =>
            {
                var system = entityManager.System<AccessReaderSystem>();

                // test empty
                var reader = new AccessReaderComponent();
                Assert.Multiple(() =>
                {
                    Assert.That(system.AreAccessTagsAllowed(new[] { "Foo" }, reader), Is.True);
                    Assert.That(system.AreAccessTagsAllowed(new[] { "Bar" }, reader), Is.True);
                    Assert.That(system.AreAccessTagsAllowed(Array.Empty<string>(), reader), Is.True);
                });

                // test deny
                reader = new AccessReaderComponent();
                reader.DenyTags.Add("A");
                Assert.Multiple(() =>
                {
                    Assert.That(system.AreAccessTagsAllowed(new[] { "Foo" }, reader), Is.True);
                    Assert.That(system.AreAccessTagsAllowed(new[] { "A" }, reader), Is.False);
                    Assert.That(system.AreAccessTagsAllowed(new[] { "A", "Foo" }, reader), Is.False);
                    Assert.That(system.AreAccessTagsAllowed(Array.Empty<string>(), reader), Is.True);
                });

                // test one list
                reader = new AccessReaderComponent();
                reader.AccessLists.Add(new HashSet<string> { "A" });
                Assert.Multiple(() =>
                {
                    Assert.That(system.AreAccessTagsAllowed(new[] { "A" }, reader), Is.True);
                    Assert.That(system.AreAccessTagsAllowed(new[] { "B" }, reader), Is.False);
                    Assert.That(system.AreAccessTagsAllowed(new[] { "A", "B" }, reader), Is.True);
                    Assert.That(system.AreAccessTagsAllowed(Array.Empty<string>(), reader), Is.False);
                });

                // test one list - two items
                reader = new AccessReaderComponent();
                reader.AccessLists.Add(new HashSet<string> { "A", "B" });
                Assert.Multiple(() =>
                {
                    Assert.That(system.AreAccessTagsAllowed(new[] { "A" }, reader), Is.False);
                    Assert.That(system.AreAccessTagsAllowed(new[] { "B" }, reader), Is.False);
                    Assert.That(system.AreAccessTagsAllowed(new[] { "A", "B" }, reader), Is.True);
                    Assert.That(system.AreAccessTagsAllowed(Array.Empty<string>(), reader), Is.False);
                });

                // test two list
                reader = new AccessReaderComponent();
                reader.AccessLists.Add(new HashSet<string> { "A" });
                reader.AccessLists.Add(new HashSet<string> { "B", "C" });
                Assert.Multiple(() =>
                {
                    Assert.That(system.AreAccessTagsAllowed(new[] { "A" }, reader), Is.True);
                    Assert.That(system.AreAccessTagsAllowed(new[] { "B" }, reader), Is.False);
                    Assert.That(system.AreAccessTagsAllowed(new[] { "A", "B" }, reader), Is.True);
                    Assert.That(system.AreAccessTagsAllowed(new[] { "C", "B" }, reader), Is.True);
                    Assert.That(system.AreAccessTagsAllowed(new[] { "C", "B", "A" }, reader), Is.True);
                    Assert.That(system.AreAccessTagsAllowed(Array.Empty<string>(), reader), Is.False);
                });

                // test deny list
                reader = new AccessReaderComponent();
                reader.AccessLists.Add(new HashSet<string> { "A" });
                reader.DenyTags.Add("B");
                Assert.Multiple(() =>
                {
                    Assert.That(system.AreAccessTagsAllowed(new[] { "A" }, reader), Is.True);
                    Assert.That(system.AreAccessTagsAllowed(new[] { "B" }, reader), Is.False);
                    Assert.That(system.AreAccessTagsAllowed(new[] { "A", "B" }, reader), Is.False);
                    Assert.That(system.AreAccessTagsAllowed(Array.Empty<string>(), reader), Is.False);
                });
            });
            await pair.CleanReturnAsync();
        }

    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using NUnit.Framework;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Access
{
    [TestFixture]
    [TestOf(typeof(AccessReaderComponent))]
    public class AccessReaderTest : ContentIntegrationTest
    {
        [Test]
        public async Task TestTags()
        {
            var server = StartServer();
            await server.WaitAssertion(() =>
            {
                var system = EntitySystem.Get<AccessReaderSystem>();

                // test empty
                var reader = new AccessReaderComponent();
                Assert.That(system.IsAllowed(reader, new[] { "Foo" }), Is.True);
                Assert.That(system.IsAllowed(reader, new[] { "Bar" }), Is.True);
                Assert.That(system.IsAllowed(reader, new string[] { }), Is.True);

                // test deny
                reader = new AccessReaderComponent();
                reader.DenyTags.Add("A");
                Assert.That(system.IsAllowed(reader, new[] { "Foo" }), Is.True);
                Assert.That(system.IsAllowed(reader, new[] { "A" }), Is.False);
                Assert.That(system.IsAllowed(reader, new[] { "A", "Foo" }), Is.False);
                Assert.That(system.IsAllowed(reader, new string[] { }), Is.True);

                // test one list
                reader = new AccessReaderComponent();
                reader.AccessLists.Add(new HashSet<string> { "A" });
                Assert.That(system.IsAllowed(reader, new[] { "A" }), Is.True);
                Assert.That(system.IsAllowed(reader, new[] { "B" }), Is.False);
                Assert.That(system.IsAllowed(reader, new[] { "A", "B" }), Is.True);
                Assert.That(system.IsAllowed(reader, new string[] { }), Is.False);

                // test one list - two items
                reader = new AccessReaderComponent();
                reader.AccessLists.Add(new HashSet<string> { "A", "B" });
                Assert.That(system.IsAllowed(reader, new[] { "A" }), Is.False);
                Assert.That(system.IsAllowed(reader, new[] { "B" }), Is.False);
                Assert.That(system.IsAllowed(reader, new[] { "A", "B" }), Is.True);
                Assert.That(system.IsAllowed(reader, new string[] { }), Is.False);

                // test two list
                reader = new AccessReaderComponent();
                reader.AccessLists.Add(new HashSet<string> { "A" });
                reader.AccessLists.Add(new HashSet<string> { "B", "C" });
                Assert.That(system.IsAllowed(reader, new[] { "A" }), Is.True);
                Assert.That(system.IsAllowed(reader, new[] { "B" }), Is.False);
                Assert.That(system.IsAllowed(reader, new[] { "A", "B" }), Is.True);
                Assert.That(system.IsAllowed(reader, new[] { "C", "B" }), Is.True);
                Assert.That(system.IsAllowed(reader, new[] { "C", "B", "A" }), Is.True);
                Assert.That(system.IsAllowed(reader, new string[] { }), Is.False);

                // test deny list
                reader = new AccessReaderComponent();
                reader.AccessLists.Add(new HashSet<string> { "A" });
                reader.DenyTags.Add("B");
                Assert.That(system.IsAllowed(reader, new[] { "A" }), Is.True);
                Assert.That(system.IsAllowed(reader, new[] { "B" }), Is.False);
                Assert.That(system.IsAllowed(reader, new[] { "A", "B" }), Is.False);
                Assert.That(system.IsAllowed(reader, new string[] { }), Is.False);
            });
        }

    }
}

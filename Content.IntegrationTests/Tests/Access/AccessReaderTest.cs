using System.Collections.Generic;
using System.Threading.Tasks;
using Content.IntegrationTests;
using Content.Server.Access.Components;
using Content.Server.Access.Systems;
using NUnit.Framework;
using Robust.Shared.GameObjects;

namespace Content.Tests.Server.GameObjects.Components.Access
{
    [TestFixture]
    [TestOf(typeof(AccessReader))]
    public class AccessReaderTest : ContentIntegrationTest
    {
        [Test]
        public async Task TestTags()
        {
            var server = StartServerDummyTicker();
            await server.WaitAssertion(() =>
            {
                var system = EntitySystem.Get<AccessReaderSystem>();

                // test empty
                var reader = new AccessReader();
                Assert.That(system.IsAllowed(reader, new[] { "Foo" }), Is.True);
                Assert.That(system.IsAllowed(reader, new[] { "Bar" }), Is.True);
                Assert.That(system.IsAllowed(reader, new string[] { }), Is.True);

                // test deny
                reader = new AccessReader();
                reader.DenyTags.Add("A");
                Assert.That(system.IsAllowed(reader, new[] { "Foo" }), Is.True);
                Assert.That(system.IsAllowed(reader, new[] { "A" }), Is.False);
                Assert.That(system.IsAllowed(reader, new[] { "A", "Foo" }), Is.False);
                Assert.That(system.IsAllowed(reader, new string[] { }), Is.True);

                // test one list
                reader = new AccessReader();
                reader.AccessLists.Add(new HashSet<string> { "A" });
                Assert.That(system.IsAllowed(reader, new[] { "A" }), Is.True);
                Assert.That(system.IsAllowed(reader, new[] { "B" }), Is.False);
                Assert.That(system.IsAllowed(reader, new[] { "A", "B" }), Is.True);
                Assert.That(system.IsAllowed(reader, new string[] { }), Is.False);

                // test one list - two items
                reader = new AccessReader();
                reader.AccessLists.Add(new HashSet<string> { "A", "B" });
                Assert.That(system.IsAllowed(reader, new[] { "A" }), Is.False);
                Assert.That(system.IsAllowed(reader, new[] { "B" }), Is.False);
                Assert.That(system.IsAllowed(reader, new[] { "A", "B" }), Is.True);
                Assert.That(system.IsAllowed(reader, new string[] { }), Is.False);

                // test two list
                reader = new AccessReader();
                reader.AccessLists.Add(new HashSet<string> { "A" });
                reader.AccessLists.Add(new HashSet<string> { "B", "C" });
                Assert.That(system.IsAllowed(reader, new[] { "A" }), Is.True);
                Assert.That(system.IsAllowed(reader, new[] { "B" }), Is.False);
                Assert.That(system.IsAllowed(reader, new[] { "A", "B" }), Is.True);
                Assert.That(system.IsAllowed(reader, new[] { "C", "B" }), Is.True);
                Assert.That(system.IsAllowed(reader, new[] { "C", "B", "A" }), Is.True);
                Assert.That(system.IsAllowed(reader, new string[] { }), Is.False);

                // test deny list
                reader = new AccessReader();
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

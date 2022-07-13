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
    public sealed class AccessReaderTest
    {
        [Test]
        public async Task TestTags()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;

            await server.WaitAssertion(() =>
            {
                var system = EntitySystem.Get<AccessReaderSystem>();

                // test empty
                var reader = new AccessReaderComponent();
                Assert.That(system.IsAllowed(new[] { "Foo" }, reader), Is.True);
                Assert.That(system.IsAllowed(new[] { "Bar" }, reader), Is.True);
                Assert.That(system.IsAllowed(new string[] { }, reader), Is.True);

                // test deny
                reader = new AccessReaderComponent();
                reader.DenyTags.Add("A");
                Assert.That(system.IsAllowed(new[] { "Foo" }, reader), Is.True);
                Assert.That(system.IsAllowed(new[] { "A" }, reader), Is.False);
                Assert.That(system.IsAllowed(new[] { "A", "Foo" }, reader), Is.False);
                Assert.That(system.IsAllowed(new string[] { }, reader), Is.True);

                // test one list
                reader = new AccessReaderComponent();
                reader.AccessLists.Add(new HashSet<string> { "A" });
                Assert.That(system.IsAllowed(new[] { "A" }, reader), Is.True);
                Assert.That(system.IsAllowed(new[] { "B" }, reader), Is.False);
                Assert.That(system.IsAllowed(new[] { "A", "B" }, reader), Is.True);
                Assert.That(system.IsAllowed(new string[] { }, reader), Is.False);

                // test one list - two items
                reader = new AccessReaderComponent();
                reader.AccessLists.Add(new HashSet<string> { "A", "B" });
                Assert.That(system.IsAllowed(new[] { "A" }, reader), Is.False);
                Assert.That(system.IsAllowed(new[] { "B" }, reader), Is.False);
                Assert.That(system.IsAllowed(new[] { "A", "B" }, reader), Is.True);
                Assert.That(system.IsAllowed(new string[] { }, reader), Is.False);

                // test two list
                reader = new AccessReaderComponent();
                reader.AccessLists.Add(new HashSet<string> { "A" });
                reader.AccessLists.Add(new HashSet<string> { "B", "C" });
                Assert.That(system.IsAllowed(new[] { "A" }, reader), Is.True);
                Assert.That(system.IsAllowed(new[] { "B" }, reader), Is.False);
                Assert.That(system.IsAllowed(new[] { "A", "B" }, reader), Is.True);
                Assert.That(system.IsAllowed(new[] { "C", "B" }, reader), Is.True);
                Assert.That(system.IsAllowed(new[] { "C", "B", "A" }, reader), Is.True);
                Assert.That(system.IsAllowed(new string[] { }, reader), Is.False);

                // test deny list
                reader = new AccessReaderComponent();
                reader.AccessLists.Add(new HashSet<string> { "A" });
                reader.DenyTags.Add("B");
                Assert.That(system.IsAllowed(new[] { "A" }, reader), Is.True);
                Assert.That(system.IsAllowed(new[] { "B" }, reader), Is.False);
                Assert.That(system.IsAllowed(new[] { "A", "B" }, reader), Is.False);
                Assert.That(system.IsAllowed(new string[] { }, reader), Is.False);
            });
            await pairTracker.CleanReturnAsync();
        }

    }
}

using System.Collections.Generic;
using Content.Server.Access.Components;
using Content.Server.Access.Systems;
using NUnit.Framework;
using Robust.Shared.GameObjects;

namespace Content.Tests.Server.GameObjects.Components.Access
{
    [TestFixture]
    [TestOf(typeof(AccessReader))]
    public class AccessReaderTest : ContentUnitTest
    {
        [Test]
        public void TestEmpty()
        {
            var reader = new AccessReader();
            var accessReader = EntitySystem.Get<AccessReaderSystem>();

            Assert.That(accessReader.IsAllowed(reader, new[] {"Foo"}), Is.True);
            Assert.That(accessReader.IsAllowed(reader, new[] {"Bar"}), Is.True);
            Assert.That(accessReader.IsAllowed(reader, new string[] {}), Is.True);
        }

        [Test]
        public void TestDeny()
        {
            var reader = new AccessReader();
            var accessReader = EntitySystem.Get<AccessReaderSystem>();

            reader.DenyTags.Add("A");

            Assert.That(accessReader.IsAllowed(reader, new[] {"Foo"}), Is.True);
            Assert.That(accessReader.IsAllowed(reader, new[] {"A"}), Is.False);
            Assert.That(accessReader.IsAllowed(reader, new[] {"A", "Foo"}), Is.False);
            Assert.That(accessReader.IsAllowed(reader, new string[] {}), Is.True);
        }

        [Test]
        public void TestOneList()
        {
            var reader = new AccessReader();
            var accessReader = EntitySystem.Get<AccessReaderSystem>();

            reader.AccessLists.Add(new HashSet<string> {"A"});

            Assert.That(accessReader.IsAllowed(reader, new[] {"A"}), Is.True);
            Assert.That(accessReader.IsAllowed(reader, new[] {"B"}), Is.False);
            Assert.That(accessReader.IsAllowed(reader, new[] {"A", "B"}), Is.True);
            Assert.That(accessReader.IsAllowed(reader, new string[] {}), Is.False);
        }

        [Test]
        public void TestOneListTwoItems()
        {
            var reader = new AccessReader();
            var accessReader = EntitySystem.Get<AccessReaderSystem>();

            reader.AccessLists.Add(new HashSet<string> {"A", "B"});

            Assert.That(accessReader.IsAllowed(reader, new[] {"A"}), Is.False);
            Assert.That(accessReader.IsAllowed(reader, new[] {"B"}), Is.False);
            Assert.That(accessReader.IsAllowed(reader, new[] {"A", "B"}), Is.True);
            Assert.That(accessReader.IsAllowed(reader, new string[] {}), Is.False);
        }

        [Test]
        public void TestTwoList()
        {
            var reader = new AccessReader();
            var system = EntitySystem.Get<AccessReaderSystem>();

            reader.AccessLists.Add(new HashSet<string> {"A"});
            reader.AccessLists.Add(new HashSet<string> {"B", "C"});

            Assert.That(system.IsAllowed(reader, new[] {"A"}), Is.True);
            Assert.That(system.IsAllowed(reader, new[] {"B"}), Is.False);
            Assert.That(system.IsAllowed(reader, new[] {"A", "B"}), Is.True);
            Assert.That(system.IsAllowed(reader, new[] {"C", "B"}), Is.True);
            Assert.That(system.IsAllowed(reader, new[] {"C", "B", "A"}), Is.True);
            Assert.That(system.IsAllowed(reader, new string[] {}), Is.False);
        }

        [Test]
        public void TestDenyList()
        {
            var reader = new AccessReader();
            var accessReader = EntitySystem.Get<AccessReaderSystem>();

            reader.AccessLists.Add(new HashSet<string> {"A"});
            reader.DenyTags.Add("B");

            Assert.That(accessReader.IsAllowed(reader, new[] {"A"}), Is.True);
            Assert.That(accessReader.IsAllowed(reader, new[] {"B"}), Is.False);
            Assert.That(accessReader.IsAllowed(reader, new[] {"A", "B"}), Is.False);
            Assert.That(accessReader.IsAllowed(reader, new string[] {}), Is.False);
        }
    }
}

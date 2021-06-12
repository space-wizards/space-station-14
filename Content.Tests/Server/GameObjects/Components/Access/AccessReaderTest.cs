using System.Collections.Generic;
using Content.Server.Access.Components;
using NUnit.Framework;

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

            Assert.That(reader.IsAllowed(new[] {"Foo"}), Is.True);
            Assert.That(reader.IsAllowed(new[] {"Bar"}), Is.True);
            Assert.That(reader.IsAllowed(new string[] {}), Is.True);
        }

        [Test]
        public void TestDeny()
        {
            var reader = new AccessReader();
            reader.DenyTags.Add("A");

            Assert.That(reader.IsAllowed(new[] {"Foo"}), Is.True);
            Assert.That(reader.IsAllowed(new[] {"A"}), Is.False);
            Assert.That(reader.IsAllowed(new[] {"A", "Foo"}), Is.False);
            Assert.That(reader.IsAllowed(new string[] {}), Is.True);
        }

        [Test]
        public void TestOneList()
        {
            var reader = new AccessReader();
            reader.AccessLists.Add(new HashSet<string> {"A"});

            Assert.That(reader.IsAllowed(new[] {"A"}), Is.True);
            Assert.That(reader.IsAllowed(new[] {"B"}), Is.False);
            Assert.That(reader.IsAllowed(new[] {"A", "B"}), Is.True);
            Assert.That(reader.IsAllowed(new string[] {}), Is.False);
        }

        [Test]
        public void TestOneListTwoItems()
        {
            var reader = new AccessReader();
            reader.AccessLists.Add(new HashSet<string> {"A", "B"});

            Assert.That(reader.IsAllowed(new[] {"A"}), Is.False);
            Assert.That(reader.IsAllowed(new[] {"B"}), Is.False);
            Assert.That(reader.IsAllowed(new[] {"A", "B"}), Is.True);
            Assert.That(reader.IsAllowed(new string[] {}), Is.False);
        }

        [Test]
        public void TestTwoList()
        {
            var reader = new AccessReader();
            reader.AccessLists.Add(new HashSet<string> {"A"});
            reader.AccessLists.Add(new HashSet<string> {"B", "C"});

            Assert.That(reader.IsAllowed(new[] {"A"}), Is.True);
            Assert.That(reader.IsAllowed(new[] {"B"}), Is.False);
            Assert.That(reader.IsAllowed(new[] {"A", "B"}), Is.True);
            Assert.That(reader.IsAllowed(new[] {"C", "B"}), Is.True);
            Assert.That(reader.IsAllowed(new[] {"C", "B", "A"}), Is.True);
            Assert.That(reader.IsAllowed(new string[] {}), Is.False);
        }

        [Test]
        public void TestDenyList()
        {
            var reader = new AccessReader();
            reader.AccessLists.Add(new HashSet<string> {"A"});
            reader.DenyTags.Add("B");

            Assert.That(reader.IsAllowed(new[] {"A"}), Is.True);
            Assert.That(reader.IsAllowed(new[] {"B"}), Is.False);
            Assert.That(reader.IsAllowed(new[] {"A", "B"}), Is.False);
            Assert.That(reader.IsAllowed(new string[] {}), Is.False);
        }
    }
}

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
        public async Task TestTags()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            var entityManager = server.ResolveDependency<IEntityManager>();


            await server.WaitAssertion(() =>
            {
                var system = entityManager.System<AccessReaderSystem>();

                // test empty
                var reader = new Entity<AccessReaderComponent>();
                Assert.Multiple(() =>
                {
                    Assert.That(system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "Foo" }, reader), Is.True);
                    Assert.That(system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "Bar" }, reader), Is.True);
                    Assert.That(system.AreAccessTagsAllowed(Array.Empty<ProtoId<AccessLevelPrototype>>(), reader), Is.True);
                });

                // test deny
                reader = new Entity<AccessReaderComponent>();
                system.AddDenyTag(reader, "A");
                Assert.Multiple(() =>
                {
                    Assert.That(system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "Foo" }, reader), Is.True);
                    Assert.That(system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "A" }, reader), Is.False);
                    Assert.That(system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "A", "Foo" }, reader), Is.False);
                    Assert.That(system.AreAccessTagsAllowed(Array.Empty<ProtoId<AccessLevelPrototype>>(), reader), Is.True);
                });

                // test one list
                reader = new Entity<AccessReaderComponent>();
                system.AddAccess(reader, "A");
                Assert.Multiple(() =>
                {
                    Assert.That(system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "A" }, reader), Is.True);
                    Assert.That(system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "B" }, reader), Is.False);
                    Assert.That(system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "A", "B" }, reader), Is.True);
                    Assert.That(system.AreAccessTagsAllowed(Array.Empty<ProtoId<AccessLevelPrototype>>(), reader), Is.False);
                });

                // test one list - two items
                reader = new Entity<AccessReaderComponent>();
                system.AddAccess(reader, new HashSet<ProtoId<AccessLevelPrototype>> { "A", "B" });
                Assert.Multiple(() =>
                {
                    Assert.That(system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "A" }, reader), Is.False);
                    Assert.That(system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "B" }, reader), Is.False);
                    Assert.That(system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "A", "B" }, reader), Is.True);
                    Assert.That(system.AreAccessTagsAllowed(Array.Empty<ProtoId<AccessLevelPrototype>>(), reader), Is.False);
                });

                // test two list
                reader = new Entity<AccessReaderComponent>();
                var accesses = new List<HashSet<ProtoId<AccessLevelPrototype>>>() {
                    new HashSet<ProtoId<AccessLevelPrototype>> () { "A" },
                    new HashSet<ProtoId<AccessLevelPrototype>> () { "B", "C" }
                };
                system.AddAccesses(reader, accesses);
                Assert.Multiple(() =>
                {
                    Assert.That(system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "A" }, reader), Is.True);
                    Assert.That(system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "B" }, reader), Is.False);
                    Assert.That(system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "A", "B" }, reader), Is.True);
                    Assert.That(system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "C", "B" }, reader), Is.True);
                    Assert.That(system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "C", "B", "A" }, reader), Is.True);
                    Assert.That(system.AreAccessTagsAllowed(Array.Empty<ProtoId<AccessLevelPrototype>>(), reader), Is.False);
                });

                // test deny list
                reader = new Entity<AccessReaderComponent>();
                system.AddAccess(reader, new HashSet<ProtoId<AccessLevelPrototype>> { "A" });
                system.AddDenyTag(reader, "B");
                Assert.Multiple(() =>
                {
                    Assert.That(system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "A" }, reader), Is.True);
                    Assert.That(system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "B" }, reader), Is.False);
                    Assert.That(system.AreAccessTagsAllowed(new List<ProtoId<AccessLevelPrototype>> { "A", "B" }, reader), Is.False);
                    Assert.That(system.AreAccessTagsAllowed(Array.Empty<ProtoId<AccessLevelPrototype>>(), reader), Is.False);
                });
            });
            await pair.CleanReturnAsync();
        }

    }
}

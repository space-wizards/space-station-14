#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Body.Behavior;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Part;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.Body
{
    [TestFixture]
    [TestOf(typeof(SharedBodyComponent))]
    [TestOf(typeof(SharedBodyPartComponent))]
    [TestOf(typeof(SharedMechanismComponent))]
    [TestOf(typeof(MechanismBehavior))]
    public class MechanismBehaviorEventsTest : ContentIntegrationTest
    {
        private class TestMechanismBehavior : MechanismBehavior
        {
            public bool WasAddedToBody;
            public bool WasAddedToPart;
            public bool WasAddedToPartInBody;
            public bool WasRemovedFromBody;
            public bool WasRemovedFromPart;
            public bool WasRemovedFromPartInBody;

            public bool NoAdded()
            {
                return !WasAddedToBody && !WasAddedToPart && !WasAddedToPartInBody;
            }

            public bool NoRemoved()
            {
                return !WasRemovedFromBody && !WasRemovedFromPart && !WasRemovedFromPartInBody;
            }

            public void ResetAdded()
            {
                WasAddedToBody = false;
                WasAddedToPart = false;
                WasAddedToPartInBody = false;
            }

            public void ResetRemoved()
            {
                WasRemovedFromBody = false;
                WasRemovedFromPart = false;
                WasRemovedFromPartInBody = false;
            }

            public void ResetAll()
            {
                ResetAdded();
                ResetRemoved();
            }

            protected override void OnAddedToBody(IBody body)
            {
                base.OnAddedToBody(body);

                WasAddedToBody = true;
            }

            protected override void OnAddedToPart(IBodyPart part)
            {
                base.OnAddedToPart(part);

                WasAddedToPart = true;
            }

            protected override void OnAddedToPartInBody(IBody body, IBodyPart part)
            {
                base.OnAddedToPartInBody(body, part);

                WasAddedToPartInBody = true;
            }

            protected override void OnRemovedFromBody(IBody old)
            {
                base.OnRemovedFromBody(old);

                WasRemovedFromBody = true;
            }

            protected override void OnRemovedFromPart(IBodyPart old)
            {
                base.OnRemovedFromPart(old);

                WasRemovedFromPart = true;
            }

            protected override void OnRemovedFromPartInBody(IBody oldBody, IBodyPart oldPart)
            {
                base.OnRemovedFromPartInBody(oldBody, oldPart);

                WasRemovedFromPartInBody = true;
            }
        }

        private const string Prototypes = @"
- type: entity
  name: HumanBodyDummy
  id: HumanBodyDummy
  components:
  - type: Body
    template: HumanoidTemplate
    preset: HumanPreset
    centerSlot: torso
";

        [Test]
        public async Task EventsTest()
        {
            var options = new ServerContentIntegrationOption {ExtraPrototypes = Prototypes};
            var server = StartServerDummyTicker(options);

            await server.WaitAssertion(() =>
            {
                var mapManager = IoCManager.Resolve<IMapManager>();

                var mapId = mapManager.CreateMap();

                var entityManager = IoCManager.Resolve<IEntityManager>();
                var human = entityManager.SpawnEntity("HumanBodyDummy", new MapCoordinates(Vector2.Zero, mapId));

                Assert.That(human.TryGetComponent(out IBody? body));
                Assert.NotNull(body);

                var centerPart = body!.CenterPart;
                Assert.NotNull(centerPart);

                Assert.That(body.TryGetSlot(centerPart!, out var centerSlot));
                Assert.NotNull(centerSlot);

                var mechanism = centerPart!.Mechanisms.First();
                Assert.NotNull(mechanism);

                mechanism.EnsureBehavior<TestMechanismBehavior>(out var behavior);
                Assert.False(behavior.WasAddedToBody);
                Assert.False(behavior.WasAddedToPart);
                Assert.That(behavior.WasAddedToPartInBody);
                Assert.That(behavior.NoRemoved);

                behavior.ResetAll();

                Assert.That(behavior.NoAdded);
                Assert.That(behavior.NoRemoved);

                centerPart.RemoveMechanism(mechanism);

                Assert.That(behavior.NoAdded);
                Assert.False(behavior.WasRemovedFromBody);
                Assert.False(behavior.WasRemovedFromPart);
                Assert.That(behavior.WasRemovedFromPartInBody);

                behavior.ResetAll();

                centerPart.TryAddMechanism(mechanism, true);

                Assert.False(behavior.WasAddedToBody);
                Assert.False(behavior.WasAddedToPart);
                Assert.That(behavior.WasAddedToPartInBody);
                Assert.That(behavior.NoRemoved());

                behavior.ResetAll();

                body.RemovePart(centerPart);

                Assert.That(behavior.NoAdded);
                Assert.That(behavior.WasRemovedFromBody);
                Assert.False(behavior.WasRemovedFromPart);
                Assert.False(behavior.WasRemovedFromPartInBody);

                behavior.ResetAll();

                centerPart.RemoveMechanism(mechanism);

                Assert.That(behavior.NoAdded);
                Assert.False(behavior.WasRemovedFromBody);
                Assert.That(behavior.WasRemovedFromPart);
                Assert.False(behavior.WasRemovedFromPartInBody);

                behavior.ResetAll();

                centerPart.TryAddMechanism(mechanism, true);

                Assert.False(behavior.WasAddedToBody);
                Assert.That(behavior.WasAddedToPart);
                Assert.False(behavior.WasAddedToPartInBody);
                Assert.That(behavior.NoRemoved);

                behavior.ResetAll();

                body.SetPart(centerSlot!.Id, centerPart);

                Assert.That(behavior.WasAddedToBody);
                Assert.False(behavior.WasAddedToPart);
                Assert.False(behavior.WasAddedToPartInBody);
                Assert.That(behavior.NoRemoved);
            });
        }
    }
}

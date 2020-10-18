#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Behavior;
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Part;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Body
{
    [TestFixture]
    [TestOf(typeof(SharedBodyComponent))]
    [TestOf(typeof(SharedBodyPartComponent))]
    [TestOf(typeof(SharedMechanismComponent))]
    [TestOf(typeof(MechanismBehaviorComponent))]
    public class MechanismBehaviorEventsTest : ContentIntegrationTest
    {
        [RegisterComponent]
        private class TestBehaviorComponent : MechanismBehaviorComponent
        {
            public override string Name => nameof(MechanismBehaviorEventsTest) + "TestBehavior";

            public bool WasAddedToBody;
            public bool WasAddedToPart;
            public bool WasAddedToPartInBody;
            public bool WasRemovedFromBody;
            public bool WasRemovedFromPart;
            public bool WasRemovedFromPartInBody;

            public override void Update(float frameTime) { }

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

        [Test]
        public async Task EventsTest()
        {
            var server = StartServerDummyTicker(new ServerContentIntegrationOption
            {
                ContentBeforeIoC = () =>
                {
                    IoCManager.Resolve<IComponentFactory>().Register<TestBehaviorComponent>();
                }
            });

            await server.WaitAssertion(() =>
            {
                var mapManager = IoCManager.Resolve<IMapManager>();

                var mapId = new MapId(0);
                mapManager.CreateNewMapEntity(mapId);

                var entityManager = IoCManager.Resolve<IEntityManager>();
                var human = entityManager.SpawnEntity("HumanMob_Content", MapCoordinates.Nullspace);

                Assert.That(human.TryGetComponent(out IBody? body));
                Assert.NotNull(body);

                var centerPart = body!.CenterPart();
                Assert.NotNull(centerPart);

                Assert.That(body.TryGetSlot(centerPart!, out var centerSlot));
                Assert.NotNull(centerSlot);

                var mechanism = centerPart!.Mechanisms.First();
                Assert.NotNull(mechanism);

                var component = mechanism.Owner.AddComponent<TestBehaviorComponent>();
                Assert.False(component.WasAddedToBody);
                Assert.False(component.WasAddedToPart);
                Assert.That(component.WasAddedToPartInBody);
                Assert.That(component.NoRemoved);

                component.ResetAll();

                Assert.That(component.NoAdded);
                Assert.That(component.NoRemoved);

                centerPart.RemoveMechanism(mechanism);

                Assert.That(component.NoAdded);
                Assert.False(component.WasRemovedFromBody);
                Assert.False(component.WasRemovedFromPart);
                Assert.That(component.WasRemovedFromPartInBody);

                component.ResetAll();

                centerPart.TryAddMechanism(mechanism, true);

                Assert.False(component.WasAddedToBody);
                Assert.False(component.WasAddedToPart);
                Assert.That(component.WasAddedToPartInBody);
                Assert.That(component.NoRemoved());

                component.ResetAll();

                body.RemovePart(centerPart);

                Assert.That(component.NoAdded);
                Assert.That(component.WasRemovedFromBody);
                Assert.False(component.WasRemovedFromPart);
                Assert.False(component.WasRemovedFromPartInBody);

                component.ResetAll();

                centerPart.RemoveMechanism(mechanism);

                Assert.That(component.NoAdded);
                Assert.False(component.WasRemovedFromBody);
                Assert.That(component.WasRemovedFromPart);
                Assert.False(component.WasRemovedFromPartInBody);

                component.ResetAll();

                centerPart.TryAddMechanism(mechanism, true);

                Assert.False(component.WasAddedToBody);
                Assert.That(component.WasAddedToPart);
                Assert.False(component.WasAddedToPartInBody);
                Assert.That(component.NoRemoved);

                component.ResetAll();

                body.TryAddPart(centerSlot!, centerPart, true);

                Assert.That(component.WasAddedToBody);
                Assert.False(component.WasAddedToPart);
                Assert.False(component.WasAddedToPartInBody);
                Assert.That(component.NoRemoved);
            });
        }
    }
}

using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.EntitySystems.Click;
using Content.Shared.Interfaces.GameObjects.Components;
using NUnit.Framework;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Reflection;
using System.Threading.Tasks;

namespace Content.IntegrationTests.Tests.Interaction.Click
{
    [TestFixture]
    [TestOf(typeof(InteractionSystem))]
    public class InteractionSystemTests : ContentIntegrationTest
    {
        [Reflect(false)]
        private class TestAttackEntitySystem : EntitySystem
        {
            public EntityEventHandler<AttackEvent> AttackEvent;
            public EntityEventHandler<InteractUsingEvent> InteractUsingEvent;
            public EntityEventHandler<AttackHandEvent> InteractHandEvent;

            public override void Initialize()
            {
                base.Initialize();
                SubscribeLocalEvent<AttackEvent>((e) => AttackEvent?.Invoke(e));
                SubscribeLocalEvent<InteractUsingEvent>((e) => InteractUsingEvent?.Invoke(e));
                SubscribeLocalEvent<AttackHandEvent>((e) => InteractHandEvent?.Invoke(e));
            }

            public override void Shutdown()
            {
                base.Shutdown();
                UnsubscribeLocalEvent<AttackEvent>();
                UnsubscribeLocalEvent<InteractUsingEvent>();
                UnsubscribeLocalEvent<AttackHandEvent>();
            }
        }

        [Test]
        public async Task InsideContainerInteractionBlockTest()
        {
            var server = StartServerDummyTicker(new ServerContentIntegrationOption
            {
                ContentBeforeIoC = () =>
                {
                    IoCManager.Resolve<IEntitySystemManager>().LoadExtraSystemType<TestAttackEntitySystem>();
                }
            });

            await server.WaitIdleAsync();

            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();

            IEntity origin = null;
            IEntity other = null;
            IEntity containerEntity = null;
            IContainer container = null;

            server.Assert(() =>
            {
                var mapId = mapManager.CreateMap();
                var coordinates = new MapCoordinates(Vector2.Zero, mapId);

                origin = entityManager.SpawnEntity(null, coordinates);
                origin.EnsureComponent<HandsComponent>().AddHand("hand");
                other = entityManager.SpawnEntity(null, coordinates);
                containerEntity = entityManager.SpawnEntity(null, coordinates);
                container = ContainerHelpers.EnsureContainer<Container>(containerEntity, "InteractionTestContainer");
            });

            await server.WaitIdleAsync();

            var attack = false;
            var interactUsing = false;
            var interactHand = false;
            server.Assert(() =>
            {
                Assert.That(container.Insert(origin));
                Assert.That(origin.Transform.Parent!.Owner, Is.EqualTo(containerEntity));

                var entitySystemManager = IoCManager.Resolve<IEntitySystemManager>();
                Assert.That(entitySystemManager.TryGetEntitySystem<InteractionSystem>(out var interactionSystem));

                Assert.That(entitySystemManager.TryGetEntitySystem<TestAttackEntitySystem>(out var testAttackEntitySystem));
                testAttackEntitySystem.AttackEvent = (ev) =>
                {
                    Assert.That(ev.Target, Is.EqualTo(containerEntity.Uid));
                    attack = true;
                };
                testAttackEntitySystem.InteractUsingEvent = (ev) =>
                {
                    Assert.That(ev.Target, Is.EqualTo(containerEntity));
                    interactUsing = true;
                };
                testAttackEntitySystem.InteractHandEvent = (ev) =>
                {
                    Assert.That(ev.Target, Is.EqualTo(containerEntity));
                    interactHand = true;
                };

                interactionSystem.DoAttack(origin, other.Transform.Coordinates, false, other.Uid);
                interactionSystem.UserInteraction(origin, other.Transform.Coordinates, other.Uid);
                Assert.That(attack, Is.False);
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand, Is.False);

                interactionSystem.DoAttack(origin, containerEntity.Transform.Coordinates, false, containerEntity.Uid);
                interactionSystem.UserInteraction(origin, containerEntity.Transform.Coordinates, containerEntity.Uid);
                Assert.That(attack);
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand);

                var itemEntity = entityManager.SpawnEntity(null, origin.Transform.Coordinates);
                var item = itemEntity.EnsureComponent<ItemComponent>();

                Assert.That(origin.TryGetComponent<HandsComponent>(out var hands));
                hands.PutInHand(item);

                interactionSystem.UserInteraction(origin, other.Transform.Coordinates, other.Uid);
                Assert.That(interactUsing, Is.False);

                interactionSystem.UserInteraction(origin, containerEntity.Transform.Coordinates, containerEntity.Uid);
                Assert.That(interactUsing, Is.True);
            });
        }
    }
}

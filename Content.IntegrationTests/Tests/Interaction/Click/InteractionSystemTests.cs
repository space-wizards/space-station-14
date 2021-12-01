using System.Threading.Tasks;
using Content.Server.Hands.Components;
using Content.Server.Interaction;
using Content.Server.Items;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Weapons.Melee;
using NUnit.Framework;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Reflection;

namespace Content.IntegrationTests.Tests.Interaction.Click
{
    [TestFixture]
    [TestOf(typeof(InteractionSystem))]
    public class InteractionSystemTests : ContentIntegrationTest
    {
        const string PROTOTYPES = @"
- type: entity
  id: DummyDebugWall
  components:
  - type: Physics
    bodyType: Dynamic
    fixtures:
    - shape:
        !type:PhysShapeAabb
          bounds: ""-0.25,-0.25,0.25,0.25""
      layer:
      - MobMask
      mask:
      - MobMask
";

        [Test]
        public async Task InteractionTest()
        {
            var server = StartServer(new ServerContentIntegrationOption
            {
                ContentBeforeIoC = () =>
                {
                    IoCManager.Resolve<IEntitySystemManager>().LoadExtraSystemType<TestInteractionSystem>();
                }
            });

            await server.WaitIdleAsync();

            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();

            var mapId = MapId.Nullspace;
            var coords = MapCoordinates.Nullspace;
            server.Assert(() =>
            {
                mapId = mapManager.CreateMap();
                coords = new MapCoordinates(Vector2.Zero, mapId);
            });

            await server.WaitIdleAsync();
            IEntity user = null;
            IEntity target = null;
            IEntity item = null;

            server.Assert(() =>
            {
                user = entityManager.SpawnEntity(null, coords);
                user.EnsureComponent<HandsComponent>().AddHand("hand", HandLocation.Left);
                target = entityManager.SpawnEntity(null, coords);
                item = entityManager.SpawnEntity(null, coords);
                item.EnsureComponent<ItemComponent>();
            });

            await server.WaitRunTicks(1);

            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            Assert.That(entitySystemManager.TryGetEntitySystem<InteractionSystem>(out var interactionSystem));
            Assert.That(entitySystemManager.TryGetEntitySystem<TestInteractionSystem>(out var testInteractionSystem));

            var attack = false;
            var interactUsing = false;
            var interactHand = false;
            server.Assert(() =>
            {
                testInteractionSystem.AttackEvent    = (_, _, ev) => { Assert.That(ev.Target, Is.EqualTo(target.Uid)); attack = true; };
                testInteractionSystem.InteractUsingEvent   = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactUsing = true; };
                testInteractionSystem.InteractHandEvent    = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactHand = true; };

                interactionSystem.DoAttack(user, target.Transform.Coordinates, false, target.Uid);
                interactionSystem.UserInteraction(user, target.Transform.Coordinates, target.Uid);
                Assert.That(attack);
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand);

                Assert.That(user.TryGetComponent<HandsComponent>(out var hands));
                Assert.That(hands.PutInHand(item.GetComponent<ItemComponent>()));

                interactionSystem.UserInteraction(user, target.Transform.Coordinates, target.Uid);
                Assert.That(interactUsing);
            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task InteractionObstructionTest()
        {
            var server = StartServer(new ServerContentIntegrationOption
            {
                ContentBeforeIoC = () =>
                {
                    IoCManager.Resolve<IEntitySystemManager>().LoadExtraSystemType<TestInteractionSystem>();
                },
                ExtraPrototypes = PROTOTYPES
            });

            await server.WaitIdleAsync();

            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();

            var mapId = MapId.Nullspace;
            var coords = MapCoordinates.Nullspace;
            server.Assert(() =>
            {
                mapId = mapManager.CreateMap();
                coords = new MapCoordinates(Vector2.Zero, mapId);
            });

            await server.WaitIdleAsync();
            IEntity user = null;
            IEntity target = null;
            IEntity item = null;
            IEntity wall = null;

            server.Assert(() =>
            {
                user = entityManager.SpawnEntity(null, coords);
                user.EnsureComponent<HandsComponent>().AddHand("hand", HandLocation.Left);
                target = entityManager.SpawnEntity(null, new MapCoordinates((1.9f, 0), mapId));
                item = entityManager.SpawnEntity(null, coords);
                item.EnsureComponent<ItemComponent>();
                wall = entityManager.SpawnEntity("DummyDebugWall", new MapCoordinates((1, 0), user.Transform.MapID));
            });

            await server.WaitRunTicks(1);

            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            Assert.That(entitySystemManager.TryGetEntitySystem<InteractionSystem>(out var interactionSystem));
            Assert.That(entitySystemManager.TryGetEntitySystem<TestInteractionSystem>(out var testInteractionSystem));

            var attack = false;
            var interactUsing = false;
            var interactHand = false;
            server.Assert(() =>
            {
                testInteractionSystem.AttackEvent    = (_, _, ev) => { Assert.That(ev.Target, Is.EqualTo(target.Uid)); attack = true; };
                testInteractionSystem.InteractUsingEvent   = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactUsing = true; };
                testInteractionSystem.InteractHandEvent    = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactHand = true; };

                interactionSystem.DoAttack(user, target.Transform.Coordinates, false, target.Uid);
                interactionSystem.UserInteraction(user, target.Transform.Coordinates, target.Uid);
                Assert.That(attack, Is.False);
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand, Is.False);

                Assert.That(user.TryGetComponent<HandsComponent>(out var hands));
                Assert.That(hands.PutInHand(item.GetComponent<ItemComponent>()));

                interactionSystem.UserInteraction(user, target.Transform.Coordinates, target.Uid);
                Assert.That(interactUsing, Is.False);
            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task InteractionInRangeTest()
        {
            var server = StartServer(new ServerContentIntegrationOption
            {
                ContentBeforeIoC = () =>
                {
                    IoCManager.Resolve<IEntitySystemManager>().LoadExtraSystemType<TestInteractionSystem>();
                }
            });

            await server.WaitIdleAsync();

            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();

            var mapId = MapId.Nullspace;
            var coords = MapCoordinates.Nullspace;
            server.Assert(() =>
            {
                mapId = mapManager.CreateMap();
                coords = new MapCoordinates(Vector2.Zero, mapId);
            });

            await server.WaitIdleAsync();
            IEntity user = null;
            IEntity target = null;
            IEntity item = null;

            server.Assert(() =>
            {
                user = entityManager.SpawnEntity(null, coords);
                user.EnsureComponent<HandsComponent>().AddHand("hand", HandLocation.Left);
                target = entityManager.SpawnEntity(null, new MapCoordinates((InteractionSystem.InteractionRange - 0.1f, 0), mapId));
                item = entityManager.SpawnEntity(null, coords);
                item.EnsureComponent<ItemComponent>();
            });

            await server.WaitRunTicks(1);

            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            Assert.That(entitySystemManager.TryGetEntitySystem<InteractionSystem>(out var interactionSystem));
            Assert.That(entitySystemManager.TryGetEntitySystem<TestInteractionSystem>(out var testInteractionSystem));

            var attack = false;
            var interactUsing = false;
            var interactHand = false;
            server.Assert(() =>
            {
                testInteractionSystem.AttackEvent    = (_, _, ev) => { Assert.That(ev.Target, Is.EqualTo(target.Uid)); attack = true; };
                testInteractionSystem.InteractUsingEvent   = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactUsing = true; };
                testInteractionSystem.InteractHandEvent    = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactHand = true; };

                interactionSystem.DoAttack(user, target.Transform.Coordinates, false, target.Uid);
                interactionSystem.UserInteraction(user, target.Transform.Coordinates, target.Uid);
                Assert.That(attack);
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand);

                Assert.That(user.TryGetComponent<HandsComponent>(out var hands));
                Assert.That(hands.PutInHand(item.GetComponent<ItemComponent>()));

                interactionSystem.UserInteraction(user, target.Transform.Coordinates, target.Uid);
                Assert.That(interactUsing);
            });

            await server.WaitIdleAsync();
        }


        [Test]
        public async Task InteractionOutOfRangeTest()
        {
            var server = StartServer(new ServerContentIntegrationOption
            {
                ContentBeforeIoC = () =>
                {
                    IoCManager.Resolve<IEntitySystemManager>().LoadExtraSystemType<TestInteractionSystem>();
                }
            });

            await server.WaitIdleAsync();

            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();

            var mapId = MapId.Nullspace;
            var coords = MapCoordinates.Nullspace;
            server.Assert(() =>
            {
                mapId = mapManager.CreateMap();
                coords = new MapCoordinates(Vector2.Zero, mapId);
            });

            await server.WaitIdleAsync();
            IEntity user = null;
            IEntity target = null;
            IEntity item = null;

            server.Assert(() =>
            {
                user = entityManager.SpawnEntity(null, coords);
                user.EnsureComponent<HandsComponent>().AddHand("hand", HandLocation.Left);
                target = entityManager.SpawnEntity(null, new MapCoordinates((InteractionSystem.InteractionRange, 0), mapId));
                item = entityManager.SpawnEntity(null, coords);
                item.EnsureComponent<ItemComponent>();
            });

            await server.WaitRunTicks(1);

            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            Assert.That(entitySystemManager.TryGetEntitySystem<InteractionSystem>(out var interactionSystem));
            Assert.That(entitySystemManager.TryGetEntitySystem<TestInteractionSystem>(out var testInteractionSystem));

            var attack = false;
            var interactUsing = false;
            var interactHand = false;
            server.Assert(() =>
            {
                testInteractionSystem.AttackEvent    = (_, _, ev) => { Assert.That(ev.Target, Is.EqualTo(target.Uid)); attack = true; };
                testInteractionSystem.InteractUsingEvent   = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactUsing = true; };
                testInteractionSystem.InteractHandEvent    = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactHand = true; };

                interactionSystem.DoAttack(user, target.Transform.Coordinates, false, target.Uid);
                interactionSystem.UserInteraction(user, target.Transform.Coordinates, target.Uid);
                Assert.That(attack, Is.False);
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand, Is.False);

                Assert.That(user.TryGetComponent<HandsComponent>(out var hands));
                Assert.That(hands.PutInHand(item.GetComponent<ItemComponent>()));

                interactionSystem.UserInteraction(user, target.Transform.Coordinates, target.Uid);
                Assert.That(interactUsing, Is.False);
            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task InsideContainerInteractionBlockTest()
        {
            var server = StartServer(new ServerContentIntegrationOption
            {
                ContentBeforeIoC = () =>
                {
                    IoCManager.Resolve<IEntitySystemManager>().LoadExtraSystemType<TestInteractionSystem>();
                },
                FailureLogLevel = Robust.Shared.Log.LogLevel.Error
            });

            await server.WaitIdleAsync();

            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();

            var mapId = MapId.Nullspace;
            var coords = MapCoordinates.Nullspace;
            server.Assert(() =>
            {
                mapId = mapManager.CreateMap();
                coords = new MapCoordinates(Vector2.Zero, mapId);
            });

            await server.WaitIdleAsync();
            IEntity user = null;
            IEntity target = null;
            IEntity item = null;
            IEntity containerEntity = null;
            IContainer container = null;

            server.Assert(() =>
            {
                user = entityManager.SpawnEntity(null, coords);
                user.EnsureComponent<HandsComponent>().AddHand("hand", HandLocation.Left);
                target = entityManager.SpawnEntity(null, coords);
                item = entityManager.SpawnEntity(null, coords);
                item.EnsureComponent<ItemComponent>();
                containerEntity = entityManager.SpawnEntity(null, coords);
                container = ContainerHelpers.EnsureContainer<Container>(containerEntity, "InteractionTestContainer");
            });

            await server.WaitRunTicks(1);

            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            Assert.That(entitySystemManager.TryGetEntitySystem<InteractionSystem>(out var interactionSystem));
            Assert.That(entitySystemManager.TryGetEntitySystem<TestInteractionSystem>(out var testInteractionSystem));

            await server.WaitIdleAsync();

            var attack = false;
            var interactUsing = false;
            var interactHand = false;
            server.Assert(() =>
            {
                Assert.That(container.Insert(user));
                Assert.That(user.Transform.Parent.Owner, Is.EqualTo(containerEntity));

                testInteractionSystem.AttackEvent     = (_, _, ev) => { Assert.That(ev.Target, Is.EqualTo(containerEntity.Uid)); attack = true; };
                testInteractionSystem.InteractUsingEvent    = (ev) => { Assert.That(ev.Target, Is.EqualTo(containerEntity)); interactUsing = true; };
                testInteractionSystem.InteractHandEvent     = (ev) => { Assert.That(ev.Target, Is.EqualTo(containerEntity)); interactHand = true; };

                interactionSystem.DoAttack(user, target.Transform.Coordinates, false, target.Uid);
                interactionSystem.UserInteraction(user, target.Transform.Coordinates, target.Uid);
                Assert.That(attack, Is.False);
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand, Is.False);

                interactionSystem.DoAttack(user, containerEntity.Transform.Coordinates, false, containerEntity.Uid);
                interactionSystem.UserInteraction(user, containerEntity.Transform.Coordinates, containerEntity.Uid);
                Assert.That(attack);
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand);

                Assert.That(user.TryGetComponent<HandsComponent>(out var hands));
                Assert.That(hands.PutInHand(item.GetComponent<ItemComponent>()));

                interactionSystem.UserInteraction(user, target.Transform.Coordinates, target.Uid);
                Assert.That(interactUsing, Is.False);

                interactionSystem.UserInteraction(user, containerEntity.Transform.Coordinates, containerEntity.Uid);
                Assert.That(interactUsing, Is.True);
            });

            await server.WaitIdleAsync();
        }

        [Reflect(false)]
        private class TestInteractionSystem : EntitySystem
        {
            public ComponentEventHandler<HandsComponent, ClickAttackEvent> AttackEvent;
            public EntityEventHandler<InteractUsingEvent> InteractUsingEvent;
            public EntityEventHandler<InteractHandEvent> InteractHandEvent;

            public override void Initialize()
            {
                base.Initialize();
                SubscribeLocalEvent<HandsComponent, ClickAttackEvent>((u, c, e) => AttackEvent?.Invoke(u, c, e));
                SubscribeLocalEvent<InteractUsingEvent>((e) => InteractUsingEvent?.Invoke(e));
                SubscribeLocalEvent<InteractHandEvent>((e) => InteractHandEvent?.Invoke(e));
            }
        }

    }
}

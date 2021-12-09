#nullable enable annotations
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
  - type: Fixtures
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

            var sEntities = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();

            var mapId = MapId.Nullspace;
            var coords = MapCoordinates.Nullspace;
            server.Assert(() =>
            {
                mapId = mapManager.CreateMap();
                coords = new MapCoordinates(Vector2.Zero, mapId);
            });

            await server.WaitIdleAsync();
            EntityUid user = default;
            EntityUid target = default;
            EntityUid item = default;

            server.Assert(() =>
            {
                user = sEntities.SpawnEntity(null, coords);
                user.EnsureComponent<HandsComponent>().AddHand("hand", HandLocation.Left);
                target = sEntities.SpawnEntity(null, coords);
                item = sEntities.SpawnEntity(null, coords);
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
                testInteractionSystem.AttackEvent    = (_, _, ev) => { Assert.That(ev.Target, Is.EqualTo(target)); attack = true; };
                testInteractionSystem.InteractUsingEvent   = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactUsing = true; };
                testInteractionSystem.InteractHandEvent    = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactHand = true; };

                interactionSystem.DoAttack(user, sEntities.GetComponent<TransformComponent>(target).Coordinates, false, target);
                interactionSystem.UserInteraction(user, sEntities.GetComponent<TransformComponent>(target).Coordinates, target);
                Assert.That(attack);
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand);

                Assert.That(sEntities.TryGetComponent<HandsComponent>(user, out var hands));
                Assert.That(hands.PutInHand(sEntities.GetComponent<ItemComponent>(item)));

                interactionSystem.UserInteraction(user, sEntities.GetComponent<TransformComponent>(target).Coordinates, target);
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

            var sEntities = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();

            var mapId = MapId.Nullspace;
            var coords = MapCoordinates.Nullspace;
            server.Assert(() =>
            {
                mapId = mapManager.CreateMap();
                coords = new MapCoordinates(Vector2.Zero, mapId);
            });

            await server.WaitIdleAsync();
            EntityUid user = default;
            EntityUid target = default;
            EntityUid item = default;
            EntityUid wall = default;

            server.Assert(() =>
            {
                user = sEntities.SpawnEntity(null, coords);
                user.EnsureComponent<HandsComponent>().AddHand("hand", HandLocation.Left);
                target = sEntities.SpawnEntity(null, new MapCoordinates((1.9f, 0), mapId));
                item = sEntities.SpawnEntity(null, coords);
                item.EnsureComponent<ItemComponent>();
                wall = sEntities.SpawnEntity("DummyDebugWall", new MapCoordinates((1, 0), sEntities.GetComponent<TransformComponent>(user).MapID));
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
                testInteractionSystem.AttackEvent    = (_, _, ev) => { Assert.That(ev.Target, Is.EqualTo(target)); attack = true; };
                testInteractionSystem.InteractUsingEvent   = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactUsing = true; };
                testInteractionSystem.InteractHandEvent    = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactHand = true; };

                interactionSystem.DoAttack(user, sEntities.GetComponent<TransformComponent>(target).Coordinates, false, target);
                interactionSystem.UserInteraction(user, sEntities.GetComponent<TransformComponent>(target).Coordinates, target);
                Assert.That(attack, Is.False);
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand, Is.False);

                Assert.That(sEntities.TryGetComponent<HandsComponent?>(user, out var hands));
                Assert.That(hands.PutInHand(sEntities.GetComponent<ItemComponent>(item)));

                interactionSystem.UserInteraction(user, sEntities.GetComponent<TransformComponent>(target).Coordinates, target);
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

            var sEntities = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();

            var mapId = MapId.Nullspace;
            var coords = MapCoordinates.Nullspace;
            server.Assert(() =>
            {
                mapId = mapManager.CreateMap();
                coords = new MapCoordinates(Vector2.Zero, mapId);
            });

            await server.WaitIdleAsync();
            EntityUid user = default;
            EntityUid target = default;
            EntityUid item = default;

            server.Assert(() =>
            {
                user = sEntities.SpawnEntity(null, coords);
                user.EnsureComponent<HandsComponent>().AddHand("hand", HandLocation.Left);
                target = sEntities.SpawnEntity(null, new MapCoordinates((InteractionSystem.InteractionRange - 0.1f, 0), mapId));
                item = sEntities.SpawnEntity(null, coords);
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
                testInteractionSystem.AttackEvent    = (_, _, ev) => { Assert.That(ev.Target, Is.EqualTo(target)); attack = true; };
                testInteractionSystem.InteractUsingEvent   = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactUsing = true; };
                testInteractionSystem.InteractHandEvent    = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactHand = true; };

                interactionSystem.DoAttack(user, sEntities.GetComponent<TransformComponent>(target).Coordinates, false, target);
                interactionSystem.UserInteraction(user, sEntities.GetComponent<TransformComponent>(target).Coordinates, target);
                Assert.That(attack);
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand);

                Assert.That(sEntities.TryGetComponent<HandsComponent>(user, out var hands));
                Assert.That(hands.PutInHand(sEntities.GetComponent<ItemComponent>(item)));

                interactionSystem.UserInteraction(user, sEntities.GetComponent<TransformComponent>(target).Coordinates, target);
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

            var sEntities = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();

            var mapId = MapId.Nullspace;
            var coords = MapCoordinates.Nullspace;
            server.Assert(() =>
            {
                mapId = mapManager.CreateMap();
                coords = new MapCoordinates(Vector2.Zero, mapId);
            });

            await server.WaitIdleAsync();
            EntityUid user = default;
            EntityUid target = default;
            EntityUid item = default;

            server.Assert(() =>
            {
                user = sEntities.SpawnEntity(null, coords);
                user.EnsureComponent<HandsComponent>().AddHand("hand", HandLocation.Left);
                target = sEntities.SpawnEntity(null, new MapCoordinates((InteractionSystem.InteractionRange, 0), mapId));
                item = sEntities.SpawnEntity(null, coords);
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
                testInteractionSystem.AttackEvent    = (_, _, ev) => { Assert.That(ev.Target, Is.EqualTo(target)); attack = true; };
                testInteractionSystem.InteractUsingEvent   = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactUsing = true; };
                testInteractionSystem.InteractHandEvent    = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactHand = true; };

                interactionSystem.DoAttack(user, sEntities.GetComponent<TransformComponent>(target).Coordinates, false, target);
                interactionSystem.UserInteraction(user, sEntities.GetComponent<TransformComponent>(target).Coordinates, target);
                Assert.That(attack, Is.False);
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand, Is.False);

                Assert.That(sEntities.TryGetComponent<HandsComponent?>(user, out var hands));
                Assert.That(hands.PutInHand(sEntities.GetComponent<ItemComponent>(item)));

                interactionSystem.UserInteraction(user, sEntities.GetComponent<TransformComponent>(target).Coordinates, target);
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

            var sEntities = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();

            var mapId = MapId.Nullspace;
            var coords = MapCoordinates.Nullspace;
            server.Assert(() =>
            {
                mapId = mapManager.CreateMap();
                coords = new MapCoordinates(Vector2.Zero, mapId);
            });

            await server.WaitIdleAsync();
            EntityUid user = default;
            EntityUid target = default;
            EntityUid item = default;
            EntityUid containerEntity = default;
            IContainer container = null;

            server.Assert(() =>
            {
                user = sEntities.SpawnEntity(null, coords);
                user.EnsureComponent<HandsComponent>().AddHand("hand", HandLocation.Left);
                target = sEntities.SpawnEntity(null, coords);
                item = sEntities.SpawnEntity(null, coords);
                item.EnsureComponent<ItemComponent>();
                containerEntity = sEntities.SpawnEntity(null, coords);
                container = containerEntity.EnsureContainer<Container>("InteractionTestContainer");
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
                Assert.That(sEntities.GetComponent<TransformComponent>(user).Parent.Owner, Is.EqualTo(containerEntity));

                testInteractionSystem.AttackEvent     = (_, _, ev) => { Assert.That(ev.Target, Is.EqualTo(containerEntity)); attack = true; };
                testInteractionSystem.InteractUsingEvent    = (ev) => { Assert.That(ev.Target, Is.EqualTo(containerEntity)); interactUsing = true; };
                testInteractionSystem.InteractHandEvent     = (ev) => { Assert.That(ev.Target, Is.EqualTo(containerEntity)); interactHand = true; };

                interactionSystem.DoAttack(user, sEntities.GetComponent<TransformComponent>(target).Coordinates, false, target);
                interactionSystem.UserInteraction(user, sEntities.GetComponent<TransformComponent>(target).Coordinates, target);
                Assert.That(attack, Is.False);
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand, Is.False);

                interactionSystem.DoAttack(user, sEntities.GetComponent<TransformComponent>(containerEntity).Coordinates, false, containerEntity);
                interactionSystem.UserInteraction(user, sEntities.GetComponent<TransformComponent>(containerEntity).Coordinates, containerEntity);
                Assert.That(attack);
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand);

                Assert.That(sEntities.TryGetComponent<HandsComponent?>(user, out var hands));
                Assert.That(hands.PutInHand(sEntities.GetComponent<ItemComponent>(item)));

                interactionSystem.UserInteraction(user, sEntities.GetComponent<TransformComponent>(target).Coordinates, target);
                Assert.That(interactUsing, Is.False);

                interactionSystem.UserInteraction(user, sEntities.GetComponent<TransformComponent>(containerEntity).Coordinates, containerEntity);
                Assert.That(interactUsing, Is.True);
            });

            await server.WaitIdleAsync();
        }

        [Reflect(false)]
        private class TestInteractionSystem : EntitySystem
        {
            public ComponentEventHandler<HandsComponent, ClickAttackEvent>? AttackEvent;
            public EntityEventHandler<InteractUsingEvent>? InteractUsingEvent;
            public EntityEventHandler<InteractHandEvent>? InteractHandEvent;

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

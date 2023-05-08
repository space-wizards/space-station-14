#nullable enable annotations
using System.Threading.Tasks;
using Content.Server.Interaction;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using NUnit.Framework;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Reflection;

namespace Content.IntegrationTests.Tests.Interaction.Click
{
    [TestFixture]
    [TestOf(typeof(InteractionSystem))]
    public sealed class InteractionSystemTests
    {
        private const string Prototypes = @"
- type: entity
  id: DummyDebugWall
  components:
  - type: Physics
    bodyType: Dynamic
  - type: Fixtures
    fixtures:
      fix1:
        shape:
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
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var sEntities = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var sysMan = server.ResolveDependency<IEntitySystemManager>();
            var handSys = sysMan.GetEntitySystem<SharedHandsSystem>();

            var mapId = MapId.Nullspace;
            var coords = MapCoordinates.Nullspace;
            await server.WaitAssertion(() =>
            {
                mapId = mapManager.CreateMap();
                coords = new MapCoordinates(Vector2.Zero, mapId);
            });

            await server.WaitIdleAsync();
            EntityUid user = default;
            EntityUid target = default;
            EntityUid item = default;

            await server.WaitAssertion(() =>
            {
                user = sEntities.SpawnEntity(null, coords);
                user.EnsureComponent<HandsComponent>();
                handSys.AddHand(user, "hand", HandLocation.Left);
                target = sEntities.SpawnEntity(null, coords);
                item = sEntities.SpawnEntity(null, coords);
                item.EnsureComponent<ItemComponent>();
            });

            await server.WaitRunTicks(1);

            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            Assert.That(entitySystemManager.TryGetEntitySystem<InteractionSystem>(out var interactionSystem));
            Assert.That(entitySystemManager.TryGetEntitySystem<TestInteractionSystem>(out var testInteractionSystem));

            var interactUsing = false;
            var interactHand = false;
            await server.WaitAssertion(() =>
            {
                testInteractionSystem.InteractUsingEvent   = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactUsing = true; };
                testInteractionSystem.InteractHandEvent    = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactHand = true; };

                interactionSystem.UserInteraction(user, sEntities.GetComponent<TransformComponent>(target).Coordinates, target);
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand);

                Assert.That(handSys.TryPickup(user, item));

                interactionSystem.UserInteraction(user, sEntities.GetComponent<TransformComponent>(target).Coordinates, target);
                Assert.That(interactUsing);
            });

            testInteractionSystem.ClearHandlers();
            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task InteractionObstructionTest()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var sEntities = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var sysMan = server.ResolveDependency<IEntitySystemManager>();
            var handSys = sysMan.GetEntitySystem<SharedHandsSystem>();

            var mapId = MapId.Nullspace;
            var coords = MapCoordinates.Nullspace;
            await server.WaitAssertion(() =>
            {
                mapId = mapManager.CreateMap();
                coords = new MapCoordinates(Vector2.Zero, mapId);
            });

            await server.WaitIdleAsync();
            EntityUid user = default;
            EntityUid target = default;
            EntityUid item = default;
            EntityUid wall = default;

            await server.WaitAssertion(() =>
            {
                user = sEntities.SpawnEntity(null, coords);
                user.EnsureComponent<HandsComponent>();
                handSys.AddHand(user, "hand", HandLocation.Left);
                target = sEntities.SpawnEntity(null, new MapCoordinates((1.9f, 0), mapId));
                item = sEntities.SpawnEntity(null, coords);
                item.EnsureComponent<ItemComponent>();
                wall = sEntities.SpawnEntity("DummyDebugWall", new MapCoordinates((1, 0), sEntities.GetComponent<TransformComponent>(user).MapID));
            });

            await server.WaitRunTicks(1);

            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            Assert.That(entitySystemManager.TryGetEntitySystem<InteractionSystem>(out var interactionSystem));
            Assert.That(entitySystemManager.TryGetEntitySystem<TestInteractionSystem>(out var testInteractionSystem));

            var interactUsing = false;
            var interactHand = false;
            await server.WaitAssertion(() =>
            {
                testInteractionSystem.InteractUsingEvent   = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactUsing = true; };
                testInteractionSystem.InteractHandEvent    = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactHand = true; };

                interactionSystem.UserInteraction(user, sEntities.GetComponent<TransformComponent>(target).Coordinates, target);
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand, Is.False);

                Assert.That(handSys.TryPickup(user, item));

                interactionSystem.UserInteraction(user, sEntities.GetComponent<TransformComponent>(target).Coordinates, target);
                Assert.That(interactUsing, Is.False);
            });

            testInteractionSystem.ClearHandlers();
            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task InteractionInRangeTest()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;

            var sEntities = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var sysMan = server.ResolveDependency<IEntitySystemManager>();
            var handSys = sysMan.GetEntitySystem<SharedHandsSystem>();

            var mapId = MapId.Nullspace;
            var coords = MapCoordinates.Nullspace;
            await server.WaitAssertion(() =>
            {
                mapId = mapManager.CreateMap();
                coords = new MapCoordinates(Vector2.Zero, mapId);
            });

            await server.WaitIdleAsync();
            EntityUid user = default;
            EntityUid target = default;
            EntityUid item = default;

            await server.WaitAssertion(() =>
            {
                user = sEntities.SpawnEntity(null, coords);
                user.EnsureComponent<HandsComponent>();
                handSys.AddHand(user, "hand", HandLocation.Left);
                target = sEntities.SpawnEntity(null, new MapCoordinates((InteractionSystem.InteractionRange - 0.1f, 0), mapId));
                item = sEntities.SpawnEntity(null, coords);
                item.EnsureComponent<ItemComponent>();
            });

            await server.WaitRunTicks(1);

            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            Assert.That(entitySystemManager.TryGetEntitySystem<InteractionSystem>(out var interactionSystem));
            Assert.That(entitySystemManager.TryGetEntitySystem<TestInteractionSystem>(out var testInteractionSystem));

            var interactUsing = false;
            var interactHand = false;
            await server.WaitAssertion(() =>
            {
                testInteractionSystem.InteractUsingEvent   = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactUsing = true; };
                testInteractionSystem.InteractHandEvent    = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactHand = true; };

                interactionSystem.UserInteraction(user, sEntities.GetComponent<TransformComponent>(target).Coordinates, target);
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand);

                Assert.That(handSys.TryPickup(user, item));

                interactionSystem.UserInteraction(user, sEntities.GetComponent<TransformComponent>(target).Coordinates, target);
                Assert.That(interactUsing);
            });

            testInteractionSystem.ClearHandlers();
            await pairTracker.CleanReturnAsync();
        }


        [Test]
        public async Task InteractionOutOfRangeTest()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;

            var sEntities = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var sysMan = server.ResolveDependency<IEntitySystemManager>();
            var handSys = sysMan.GetEntitySystem<SharedHandsSystem>();

            var mapId = MapId.Nullspace;
            var coords = MapCoordinates.Nullspace;
            await server.WaitAssertion(() =>
            {
                mapId = mapManager.CreateMap();
                coords = new MapCoordinates(Vector2.Zero, mapId);
            });

            await server.WaitIdleAsync();
            EntityUid user = default;
            EntityUid target = default;
            EntityUid item = default;

            await server.WaitAssertion(() =>
            {
                user = sEntities.SpawnEntity(null, coords);
                user.EnsureComponent<HandsComponent>();
                handSys.AddHand(user, "hand", HandLocation.Left);
                target = sEntities.SpawnEntity(null, new MapCoordinates((SharedInteractionSystem.InteractionRange + 0.01f, 0), mapId));
                item = sEntities.SpawnEntity(null, coords);
                item.EnsureComponent<ItemComponent>();
            });

            await server.WaitRunTicks(1);

            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            Assert.That(entitySystemManager.TryGetEntitySystem<InteractionSystem>(out var interactionSystem));
            Assert.That(entitySystemManager.TryGetEntitySystem<TestInteractionSystem>(out var testInteractionSystem));

            var interactUsing = false;
            var interactHand = false;
            await server.WaitAssertion(() =>
            {
                testInteractionSystem.InteractUsingEvent   = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactUsing = true; };
                testInteractionSystem.InteractHandEvent    = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactHand = true; };

                interactionSystem.UserInteraction(user, sEntities.GetComponent<TransformComponent>(target).Coordinates, target);
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand, Is.False);

                Assert.That(handSys.TryPickup(user, item));

                interactionSystem.UserInteraction(user, sEntities.GetComponent<TransformComponent>(target).Coordinates, target);
                Assert.That(interactUsing, Is.False);
            });

            testInteractionSystem.ClearHandlers();
            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task InsideContainerInteractionBlockTest()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;

            var sEntities = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var sysMan = server.ResolveDependency<IEntitySystemManager>();
            var handSys = sysMan.GetEntitySystem<SharedHandsSystem>();
            var conSystem = sysMan.GetEntitySystem<SharedContainerSystem>();

            var mapId = MapId.Nullspace;
            var coords = MapCoordinates.Nullspace;
            await server.WaitAssertion(() =>
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

            await server.WaitAssertion(() =>
            {
                user = sEntities.SpawnEntity(null, coords);
                user.EnsureComponent<HandsComponent>();
                handSys.AddHand(user, "hand", HandLocation.Left);
                target = sEntities.SpawnEntity(null, coords);
                item = sEntities.SpawnEntity(null, coords);
                item.EnsureComponent<ItemComponent>();
                containerEntity = sEntities.SpawnEntity(null, coords);
                container = conSystem.EnsureContainer<Container>(containerEntity, "InteractionTestContainer");
            });

            await server.WaitRunTicks(1);

            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            Assert.That(entitySystemManager.TryGetEntitySystem<InteractionSystem>(out var interactionSystem));
            Assert.That(entitySystemManager.TryGetEntitySystem<TestInteractionSystem>(out var testInteractionSystem));

            await server.WaitIdleAsync();

            var interactUsing = false;
            var interactHand = false;
            await server.WaitAssertion(() =>
            {
                Assert.That(container.Insert(user));
                Assert.That(sEntities.GetComponent<TransformComponent>(user).ParentUid, Is.EqualTo(containerEntity));

                testInteractionSystem.InteractUsingEvent    = (ev) => { Assert.That(ev.Target, Is.EqualTo(containerEntity)); interactUsing = true; };
                testInteractionSystem.InteractHandEvent     = (ev) => { Assert.That(ev.Target, Is.EqualTo(containerEntity)); interactHand = true; };

                interactionSystem.UserInteraction(user, sEntities.GetComponent<TransformComponent>(target).Coordinates, target);
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand, Is.False);

                interactionSystem.UserInteraction(user, sEntities.GetComponent<TransformComponent>(containerEntity).Coordinates, containerEntity);
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand);

                Assert.That(handSys.TryPickup(user, item));

                interactionSystem.UserInteraction(user, sEntities.GetComponent<TransformComponent>(target).Coordinates, target);
                Assert.That(interactUsing, Is.False);

                interactionSystem.UserInteraction(user, sEntities.GetComponent<TransformComponent>(containerEntity).Coordinates, containerEntity);
                Assert.That(interactUsing, Is.True);
            });

            testInteractionSystem.ClearHandlers();
            await pairTracker.CleanReturnAsync();
        }

        [Reflect(false)]
        public sealed class TestInteractionSystem : EntitySystem
        {
            public EntityEventHandler<InteractUsingEvent>? InteractUsingEvent;
            public EntityEventHandler<InteractHandEvent>? InteractHandEvent;

            public override void Initialize()
            {
                base.Initialize();
                SubscribeLocalEvent<InteractUsingEvent>((e) => InteractUsingEvent?.Invoke(e));
                SubscribeLocalEvent<InteractHandEvent>((e) => InteractHandEvent?.Invoke(e));
            }

            public void ClearHandlers()
            {
                InteractUsingEvent = null;
                InteractHandEvent = null;
            }
        }

    }
}

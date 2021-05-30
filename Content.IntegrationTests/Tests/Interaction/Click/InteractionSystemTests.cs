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
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Content.IntegrationTests.Tests.Interaction.Click
{
    [TestFixture]
    [TestOf(typeof(InteractionSystem))]
    public class InteractionSystemTests : ContentIntegrationTest
    {
        const string PROTOTYPES = @"
- type: entity
  id: DummyTarget
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

        private async Task<(ServerIntegrationInstance, IEntity, IEntity, IEntity)> Startup(Vector2 targetCoords, CancellationToken token)
        {
            var server = StartServerDummyTicker(new ServerContentIntegrationOption
            {
                ContentBeforeIoC = () =>
                {
                    IoCManager.Resolve<IEntitySystemManager>().LoadExtraSystemType<TestInteractionSystem>();
                },
                ExtraPrototypes = PROTOTYPES
            });

            await server.WaitIdleAsync(cancellationToken: token);

            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();

            var mapId = MapId.Nullspace;
            var coords = MapCoordinates.Nullspace;
            server.Assert(() =>
            {
                mapId = mapManager.CreateMap();
                coords = new MapCoordinates(Vector2.Zero, mapId);
            });

            await server.WaitIdleAsync(cancellationToken: token);
            IEntity userEntity = null;
            IEntity targetEntity = null;
            IEntity itemEntity = null;

            token.Register(() =>
            {
                Console.WriteLine("Canceled");
            });

            server.Assert(() =>
            {
                userEntity = entityManager.SpawnEntity(null, coords);
                userEntity.EnsureComponent<HandsComponent>().AddHand("hand");
                targetEntity = entityManager.SpawnEntity(null, new MapCoordinates(targetCoords, mapId));
                itemEntity = entityManager.SpawnEntity(null, coords);
                itemEntity.EnsureComponent<ItemComponent>();
            });

            Console.WriteLine($"Startup {token.IsCancellationRequested} =====================================================================================================================================================================");
            server.RunTicks(10);
            await server.WaitIdleAsync(cancellationToken: token);
            Console.WriteLine($"Startup2 {token.IsCancellationRequested} =====================================================================================================================================================================");

            return (server, userEntity, targetEntity, itemEntity);
        }

        private async Task Shutdown(ServerIntegrationInstance server,CancellationTokenSource cancel, params IEntity[] entities)
        {
            var entityManager = server.ResolveDependency<IEntityManager>();

            server.Assert(() =>
            {
                foreach (var entity in entities)
                    entityManager.DeleteEntity(entity);
            });

            server.RunTicks(1);
            await server.WaitIdleAsync(cancellationToken: cancel.Token);

            Console.WriteLine($"Shutdown {cancel.IsCancellationRequested} =====================================================================================================================================================================");
            cancel.Cancel();
            cancel.Dispose();
        }

        [Test]
        public async Task InteractionTest()
        {
            CancellationTokenSource cancel = new();
            var (server, user, target, item) = await Startup(new Vector2(0, 0), cancel.Token);

            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            Assert.That(entitySystemManager.TryGetEntitySystem<InteractionSystem>(out var interactionSystem));
            Assert.That(entitySystemManager.TryGetEntitySystem<TestInteractionSystem>(out var testInteractionSystem));

            Console.WriteLine($"Test {cancel.IsCancellationRequested} =====================================================================================================================================================================");

            var attack = false;
            var interactUsing = false;
            var interactHand = false;
            server.Assert(() =>
            {
                testInteractionSystem.AttackEvent          = (ev) => { Assert.That(ev.Target, Is.EqualTo(target.Uid)); attack = true; };
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

            await server.WaitIdleAsync(cancellationToken: cancel.Token);

            await Shutdown(server, cancel, user, target, item);
        }

        [Test]
        public async Task InteractionObstructionTest()
        {
            CancellationTokenSource cancel = new();
            var (server, user, target, item) = await Startup((1.9f, 0), cancel.Token);
            IEntity wall = null;

            Console.WriteLine($"Test {cancel.IsCancellationRequested} =====================================================================================================================================================================");

            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            Assert.That(entitySystemManager.TryGetEntitySystem<InteractionSystem>(out var interactionSystem));
            Assert.That(entitySystemManager.TryGetEntitySystem<TestInteractionSystem>(out var testInteractionSystem));

            server.Assert(() =>
            {
                wall = entityManager.SpawnEntity("DummyTarget", new MapCoordinates((1, 0), user.Transform.MapID));
            });

            await server.WaitRunTicks(1);

            var attack = false;
            var interactUsing = false;
            var interactHand = false;
            server.Assert(() =>
            {
                testInteractionSystem.AttackEvent          = (ev) => { Assert.That(ev.Target, Is.EqualTo(target.Uid)); attack = true; };
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

            await server.WaitIdleAsync(cancellationToken: cancel.Token);

            await Shutdown(server, cancel, user, target, item, wall);
        }

        [Test]
        public async Task InteractionInRangeTest()
        {
            CancellationTokenSource cancel = new();
            var (server, user, target, item) = await Startup((InteractionSystem.InteractionRange - 0.1f, 0), cancel.Token);

            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            Assert.That(entitySystemManager.TryGetEntitySystem<InteractionSystem>(out var interactionSystem));
            Assert.That(entitySystemManager.TryGetEntitySystem<TestInteractionSystem>(out var testInteractionSystem));
            Console.WriteLine($"Test {cancel.IsCancellationRequested} =====================================================================================================================================================================");

            var attack = false;
            var interactUsing = false;
            var interactHand = false;
            server.Assert(() =>
            {
                testInteractionSystem.AttackEvent          = (ev) => { Assert.That(ev.Target, Is.EqualTo(target.Uid)); attack = true; };
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

            await server.WaitIdleAsync(cancellationToken: cancel.Token);

            await Shutdown(server, cancel, user, target, item);
        }


        [Test]
        public async Task InteractionOutOfRangeTest()
        {
            CancellationTokenSource cancel = new();
            var (server, user, target, item) = await Startup((InteractionSystem.InteractionRange, 0), cancel.Token);

            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            Assert.That(entitySystemManager.TryGetEntitySystem<InteractionSystem>(out var interactionSystem));
            Assert.That(entitySystemManager.TryGetEntitySystem<TestInteractionSystem>(out var testInteractionSystem));
            Console.WriteLine($"Test {cancel.IsCancellationRequested} =====================================================================================================================================================================");

            var attack = false;
            var interactUsing = false;
            var interactHand = false;
            server.Assert(() =>
            {
                testInteractionSystem.AttackEvent          = (ev) => { Assert.That(ev.Target, Is.EqualTo(target.Uid)); attack = true; };
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

            await server.WaitIdleAsync(cancellationToken: cancel.Token);

            await Shutdown(server, cancel, user, target, item);
        }

        [Test]
        public async Task InsideContainerInteractionBlockTest()
        {
            CancellationTokenSource cancel = new();
            var (server, user, target, item) = await Startup((0, 0), cancel.Token);
            IEntity containerEntity = null;
            IContainer container = null;

            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            Assert.That(entitySystemManager.TryGetEntitySystem<InteractionSystem>(out var interactionSystem));
            Assert.That(entitySystemManager.TryGetEntitySystem<TestInteractionSystem>(out var testInteractionSystem));
            Console.WriteLine($"Test {cancel.IsCancellationRequested} =====================================================================================================================================================================");

            server.Assert(() =>
            {
                containerEntity = entityManager.SpawnEntity(null, user.Transform.MapPosition);
                container = ContainerHelpers.EnsureContainer<Container>(containerEntity, "InteractionTestContainer");
            });

            await server.WaitIdleAsync(cancellationToken: cancel.Token);

            var attack = false;
            var interactUsing = false;
            var interactHand = false;
            server.Assert(() =>
            {
                Assert.That(container.Insert(user));
                Assert.That(user.Transform.Parent.Owner, Is.EqualTo(containerEntity));

                testInteractionSystem.AttackEvent           = (ev) => { Assert.That(ev.Target, Is.EqualTo(containerEntity.Uid)); attack = true; };
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

            await server.WaitIdleAsync(cancellationToken: cancel.Token);

            await Shutdown(server, cancel, user, target, item, containerEntity);
        }

        [Reflect(false)]
        private class TestInteractionSystem : EntitySystem
        {
            public EntityEventHandler<AttackEvent> AttackEvent;
            public EntityEventHandler<InteractUsingEvent> InteractUsingEvent;
            public EntityEventHandler<InteractHandEvent> InteractHandEvent;

            public override void Initialize()
            {
                base.Initialize();
                SubscribeLocalEvent<AttackEvent>((e) => AttackEvent?.Invoke(e));
                SubscribeLocalEvent<InteractUsingEvent>((e) => InteractUsingEvent?.Invoke(e));
                SubscribeLocalEvent<InteractHandEvent>((e) => InteractHandEvent?.Invoke(e));
            }
        }

    }
}

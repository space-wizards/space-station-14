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
using Robust.Shared.Physics.Broadphase;
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
        private ServerIntegrationInstance _server;

        private IEntityManager _entityManager;
        private IMapManager _mapManager;
        private IEntitySystemManager _entitySystemManager;

        private InteractionSystem _interactionSystem;
        private TestInteractionSystem _testInteractionSystem;
        private SharedBroadPhaseSystem _sharedBroadPhaseSystem;

        private MapId _mapId;
        private MapCoordinates _mapCoordinates;

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
//- type: entity
//  id: InteractionDebugWall
//  - type: Physics
//    bodyType: Static
//    fixtures:
//    - shape:
//        !type:PhysShapeAabb
//          bounds: ""-0.5,-0.5,0.5,0.5""
//      layer:
//      - MobMask
//      mask:
//      - MobMask
//";

        [OneTimeSetUp]
        public async Task Setup()
        {
            _server = StartServerDummyTicker(new ServerContentIntegrationOption
            {
                ContentBeforeIoC = () =>
                {
                    IoCManager.Resolve<IEntitySystemManager>().LoadExtraSystemType<TestInteractionSystem>();
                },
                ExtraPrototypes = PROTOTYPES
            });

            await _server.WaitIdleAsync();

            _entityManager = _server.ResolveDependency<IEntityManager>();
            _mapManager = _server.ResolveDependency<IMapManager>();
            _entitySystemManager = _server.ResolveDependency<IEntitySystemManager>();

            Assert.That(_entitySystemManager.TryGetEntitySystem<InteractionSystem>(out _interactionSystem));
            Assert.That(_entitySystemManager.TryGetEntitySystem<TestInteractionSystem>(out _testInteractionSystem));
            Assert.That(_entitySystemManager.TryGetEntitySystem<SharedBroadPhaseSystem>(out _sharedBroadPhaseSystem));

            _server.Assert(() =>
            {
                _mapId = _mapManager.CreateMap();
                _mapCoordinates = new MapCoordinates(Vector2.Zero, _mapId);
            });

            await _server.WaitIdleAsync();
        }

        private async Task<(IEntity, IEntity, IEntity)> Startup(MapCoordinates userCoords, MapCoordinates targetCoords, MapCoordinates itemCoords, CancellationToken token)
        {
            IEntity userEntity = null;
            IEntity targetEntity = null;
            IEntity itemEntity = null;

            token.Register(() =>
            {
                Console.WriteLine("Canceled");
            });

            _server.Assert(() =>
            {
                userEntity = _entityManager.SpawnEntity(null, userCoords);
                userEntity.EnsureComponent<HandsComponent>().AddHand("hand");
                targetEntity = _entityManager.SpawnEntity(null, targetCoords);
                itemEntity = _entityManager.SpawnEntity(null, itemCoords);
                itemEntity.EnsureComponent<ItemComponent>();
            });

            Console.WriteLine($"Startup {token.IsCancellationRequested} =====================================================================================================================================================================");
            _server.RunTicks(10);
            await _server.WaitIdleAsync(cancellationToken: token);
            Console.WriteLine($"Startup2 {token.IsCancellationRequested} =====================================================================================================================================================================");

            return (userEntity, targetEntity, itemEntity);
        }

        private async Task Shutdown(CancellationTokenSource cancel, params IEntity[] entities)
        {
            _server.Assert(() =>
            {
                foreach (var entity in entities)
                    _entityManager.DeleteEntity(entity);
            });

            _server.RunTicks(1);
            await _server.WaitIdleAsync(cancellationToken: cancel.Token);

            Console.WriteLine($"Shutdown {cancel.IsCancellationRequested} =====================================================================================================================================================================");
            cancel.Cancel();
            cancel.Dispose();
        }

        [Test]
        public async Task InteractionTest()
        {
            CancellationTokenSource cancel = new();
            var (user, target, item) = await Startup(_mapCoordinates, _mapCoordinates, _mapCoordinates, cancel.Token);

            Console.WriteLine($"Test {cancel.IsCancellationRequested} =====================================================================================================================================================================");

            var attack = false;
            var interactUsing = false;
            var interactHand = false;
            _server.Assert(() =>
            {
                _testInteractionSystem.AttackEvent          = (ev) => { Assert.That(ev.Target, Is.EqualTo(target.Uid)); attack = true; };
                _testInteractionSystem.InteractUsingEvent   = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactUsing = true; };
                _testInteractionSystem.InteractHandEvent    = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactHand = true; };

                _interactionSystem.DoAttack(user, target.Transform.Coordinates, false, target.Uid);
                _interactionSystem.UserInteraction(user, target.Transform.Coordinates, target.Uid);
                Assert.That(attack);
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand);

                Assert.That(user.TryGetComponent<HandsComponent>(out var hands));
                Assert.That(hands.PutInHand(item.GetComponent<ItemComponent>()));

                _interactionSystem.UserInteraction(user, target.Transform.Coordinates, target.Uid);
                Assert.That(interactUsing);
            });

            await _server.WaitIdleAsync(cancellationToken: cancel.Token);

            await Shutdown(cancel, user, target, item);
        }

        [Test]
        public async Task InteractionObstructionTest()
        {
            CancellationTokenSource cancel = new();
            var (user, target, item) = await Startup(_mapCoordinates, new MapCoordinates((1.9f, 0), _mapId), _mapCoordinates, cancel.Token);
            IEntity wall = null;

            Console.WriteLine($"Test {cancel.IsCancellationRequested} =====================================================================================================================================================================");

            _server.Assert(() =>
            {
                wall = _entityManager.SpawnEntity("DummyTarget", new MapCoordinates((1, 0), _mapId));
            });

            await _server.WaitRunTicks(1);

            var attack = false;
            var interactUsing = false;
            var interactHand = false;
            _server.Assert(() =>
            {
                _testInteractionSystem.AttackEvent          = (ev) => { Assert.That(ev.Target, Is.EqualTo(target.Uid)); attack = true; };
                _testInteractionSystem.InteractUsingEvent   = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactUsing = true; };
                _testInteractionSystem.InteractHandEvent    = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactHand = true; };

                _interactionSystem.DoAttack(user, target.Transform.Coordinates, false, target.Uid);
                _interactionSystem.UserInteraction(user, target.Transform.Coordinates, target.Uid);
                Assert.That(attack, Is.False);
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand, Is.False);

                Assert.That(user.TryGetComponent<HandsComponent>(out var hands));
                Assert.That(hands.PutInHand(item.GetComponent<ItemComponent>()));

                _interactionSystem.UserInteraction(user, target.Transform.Coordinates, target.Uid);
                Assert.That(interactUsing, Is.False);
            });

            await _server.WaitIdleAsync(cancellationToken: cancel.Token);

            await Shutdown(cancel, user, target, item, wall);
        }

        [Test]
        public async Task InteractionInRangeTest()
        {
            CancellationTokenSource cancel = new();
            var (user, target, item) = await Startup(
                _mapCoordinates,
                new MapCoordinates((InteractionSystem.InteractionRange - 0.1f, 0), _mapId),
                _mapCoordinates,
                cancel.Token);
            Console.WriteLine($"Test {cancel.IsCancellationRequested} =====================================================================================================================================================================");

            var attack = false;
            var interactUsing = false;
            var interactHand = false;
            _server.Assert(() =>
            {
                _testInteractionSystem.AttackEvent          = (ev) => { Assert.That(ev.Target, Is.EqualTo(target.Uid)); attack = true; };
                _testInteractionSystem.InteractUsingEvent   = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactUsing = true; };
                _testInteractionSystem.InteractHandEvent    = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactHand = true; };

                _interactionSystem.DoAttack(user, target.Transform.Coordinates, false, target.Uid);
                _interactionSystem.UserInteraction(user, target.Transform.Coordinates, target.Uid);
                Assert.That(attack);
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand);

                Assert.That(user.TryGetComponent<HandsComponent>(out var hands));
                Assert.That(hands.PutInHand(item.GetComponent<ItemComponent>()));

                _interactionSystem.UserInteraction(user, target.Transform.Coordinates, target.Uid);
                Assert.That(interactUsing);
            });

            await _server.WaitIdleAsync(cancellationToken: cancel.Token);

            await Shutdown(cancel, user, target, item);
        }


        [Test]
        public async Task InteractionOutOfRangeTest()
        {
            CancellationTokenSource cancel = new();
            var (user, target, item) = await Startup(
                _mapCoordinates,
                new MapCoordinates((InteractionSystem.InteractionRange, 0), _mapId),
                _mapCoordinates,
                cancel.Token);
            Console.WriteLine($"Test {cancel.IsCancellationRequested} =====================================================================================================================================================================");

            var attack = false;
            var interactUsing = false;
            var interactHand = false;
            _server.Assert(() =>
            {
                _testInteractionSystem.AttackEvent          = (ev) => { Assert.That(ev.Target, Is.EqualTo(target.Uid)); attack = true; };
                _testInteractionSystem.InteractUsingEvent   = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactUsing = true; };
                _testInteractionSystem.InteractHandEvent    = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactHand = true; };

                _interactionSystem.DoAttack(user, target.Transform.Coordinates, false, target.Uid);
                _interactionSystem.UserInteraction(user, target.Transform.Coordinates, target.Uid);
                Assert.That(attack, Is.False);
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand, Is.False);

                Assert.That(user.TryGetComponent<HandsComponent>(out var hands));
                Assert.That(hands.PutInHand(item.GetComponent<ItemComponent>()));

                _interactionSystem.UserInteraction(user, target.Transform.Coordinates, target.Uid);
                Assert.That(interactUsing, Is.False);
            });

            await _server.WaitIdleAsync(cancellationToken: cancel.Token);

            await Shutdown(cancel, user, target, item);
        }

        [Test]
        public async Task InsideContainerInteractionBlockTest()
        {
            CancellationTokenSource cancel = new();
            var (user, target, item) = await Startup(_mapCoordinates, _mapCoordinates, _mapCoordinates, cancel.Token);
            IEntity containerEntity = null;
            IContainer container = null;
            Console.WriteLine($"Test {cancel.IsCancellationRequested} =====================================================================================================================================================================");

            _server.Assert(() =>
            {
                containerEntity = _entityManager.SpawnEntity(null, _mapCoordinates);
                container = ContainerHelpers.EnsureContainer<Container>(containerEntity, "InteractionTestContainer");
            });

            await _server.WaitIdleAsync(cancellationToken: cancel.Token);

            var attack = false;
            var interactUsing = false;
            var interactHand = false;
            _server.Assert(() =>
            {
                Assert.That(container.Insert(user));
                Assert.That(user.Transform.Parent.Owner, Is.EqualTo(containerEntity));

                _testInteractionSystem.AttackEvent           = (ev) => { Assert.That(ev.Target, Is.EqualTo(containerEntity.Uid)); attack = true; };
                _testInteractionSystem.InteractUsingEvent    = (ev) => { Assert.That(ev.Target, Is.EqualTo(containerEntity)); interactUsing = true; };
                _testInteractionSystem.InteractHandEvent     = (ev) => { Assert.That(ev.Target, Is.EqualTo(containerEntity)); interactHand = true; };

                _interactionSystem.DoAttack(user, target.Transform.Coordinates, false, target.Uid);
                _interactionSystem.UserInteraction(user, target.Transform.Coordinates, target.Uid);
                Assert.That(attack, Is.False);
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand, Is.False);

                _interactionSystem.DoAttack(user, containerEntity.Transform.Coordinates, false, containerEntity.Uid);
                _interactionSystem.UserInteraction(user, containerEntity.Transform.Coordinates, containerEntity.Uid);
                Assert.That(attack);
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand);

                Assert.That(user.TryGetComponent<HandsComponent>(out var hands));
                Assert.That(hands.PutInHand(item.GetComponent<ItemComponent>()));

                _interactionSystem.UserInteraction(user, target.Transform.Coordinates, target.Uid);
                Assert.That(interactUsing, Is.False);

                _interactionSystem.UserInteraction(user, containerEntity.Transform.Coordinates, containerEntity.Uid);
                Assert.That(interactUsing, Is.True);
            });

            await _server.WaitIdleAsync(cancellationToken: cancel.Token);

            await Shutdown(cancel, user, target, item, containerEntity);
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

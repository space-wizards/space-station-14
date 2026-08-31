#nullable enable
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Interaction;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Item;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Interaction.Click;

[TestOf(typeof(InteractionSystem))]
public sealed partial class InteractionSystemTests : GameTest
{
    private const string DummyDebugWall = "DummyDebugWall";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  id: {DummyDebugWall}
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

    [SidedDependency(Side.Server)] private SharedContainerSystem _sContainerSystem = default!;
    [SidedDependency(Side.Server)] private SharedHandsSystem _sHandsSystem = default!;
    [SidedDependency(Side.Server)] private InteractionSystem _sInteractionSystem = default!;
    [SidedDependency(Side.Server)] private TestInteractionSystem _sTestInteractionSystem = default!;

    [Test]
    public async Task InteractionTest()
    {
        await Pair.CreateTestMap();
        Assert.That(TestMap, Is.Not.Null);
        var mapId = TestMap.MapId;
        var coords = TestMap.GridCoords;

        EntityUid user = default;
        EntityUid target = default;
        EntityUid item = default;

        await Server.WaitAssertion(() =>
        {
            user = SSpawnAtPosition(null, coords);
            SEntMan.EnsureComponent<HandsComponent>(user);
            SEntMan.EnsureComponent<ComplexInteractionComponent>(user);
            _sHandsSystem.AddHand(user, "hand", HandLocation.Left);
            target = SSpawnAtPosition(null, coords);
            item = SSpawnAtPosition(null, coords);
            SEntMan.EnsureComponent<ItemComponent>(item);
        });

        await Server.WaitRunTicks(1);

        var interactUsing = false;
        var interactHand = false;
        await Server.WaitAssertion(() =>
        {
            _sTestInteractionSystem.InteractUsingEvent = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactUsing = true; };
            _sTestInteractionSystem.InteractHandEvent = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactHand = true; };

            _sInteractionSystem.UserInteraction(user, SComp<TransformComponent>(target).Coordinates, target);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand);
            }

            Assert.That(_sHandsSystem.TryPickup(user, item));

            _sInteractionSystem.UserInteraction(user, SComp<TransformComponent>(target).Coordinates, target);
            Assert.That(interactUsing);
        });

        _sTestInteractionSystem.ClearHandlers();
    }

    [Test]
    public async Task InteractionObstructionTest()
    {
        await Pair.CreateTestMap();
        Assert.That(TestMap, Is.Not.Null);
        var coords = TestMap.GridCoords;

        EntityUid user = default;
        EntityUid target = default;
        EntityUid item = default;
        EntityUid wall = default;

        await Server.WaitAssertion(() =>
        {
            user = SSpawnAtPosition(null, coords);
            SEntMan.EnsureComponent<HandsComponent>(user);
            _sHandsSystem.AddHand(user, "hand", HandLocation.Left);
            target = SSpawnAtPosition(null, new EntityCoordinates(TestMap.MapUid, new Vector2(1.9f, 0)));
            item = SSpawnAtPosition(null, coords);
            SEntMan.EnsureComponent<ItemComponent>(item);
            wall = SSpawnAtPosition(DummyDebugWall, new EntityCoordinates(TestMap.MapUid, new Vector2(1, 0)));
        });

        await Server.WaitRunTicks(1);

        var interactUsing = false;
        var interactHand = false;
        await Server.WaitAssertion(() =>
        {
            _sTestInteractionSystem.InteractUsingEvent = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactUsing = true; };
            _sTestInteractionSystem.InteractHandEvent = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactHand = true; };

            _sInteractionSystem.UserInteraction(user, SComp<TransformComponent>(target).Coordinates, target);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand, Is.False);
            }

            Assert.That(_sHandsSystem.TryPickup(user, item));

            _sInteractionSystem.UserInteraction(user, SComp<TransformComponent>(target).Coordinates, target);
            Assert.That(interactUsing, Is.False);
        });

        _sTestInteractionSystem.ClearHandlers();
    }

    [Test]
    public async Task InteractionInRangeTest()
    {
        await Pair.CreateTestMap();
        Assert.That(TestMap, Is.Not.Null);
        var coords = TestMap.GridCoords;

        EntityUid user = default;
        EntityUid target = default;
        EntityUid item = default;

        await Server.WaitAssertion(() =>
        {
            user = SSpawnAtPosition(null, coords);
            SEntMan.EnsureComponent<HandsComponent>(user);
            SEntMan.EnsureComponent<ComplexInteractionComponent>(user);
            _sHandsSystem.AddHand(user, "hand", HandLocation.Left);
            target = SSpawnAtPosition(null, new EntityCoordinates(TestMap.MapUid, new Vector2(SharedInteractionSystem.InteractionRange - 0.1f, 0)));
            item = SSpawnAtPosition(null, coords);
            SEntMan.EnsureComponent<ItemComponent>(item);
        });

        await Server.WaitRunTicks(1);

        var interactUsing = false;
        var interactHand = false;
        await Server.WaitAssertion(() =>
        {
            _sTestInteractionSystem.InteractUsingEvent = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactUsing = true; };
            _sTestInteractionSystem.InteractHandEvent = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactHand = true; };

            _sInteractionSystem.UserInteraction(user, SComp<TransformComponent>(target).Coordinates, target);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand);
            }

            Assert.That(_sHandsSystem.TryPickup(user, item));

            _sInteractionSystem.UserInteraction(user, SComp<TransformComponent>(target).Coordinates, target);
            Assert.That(interactUsing);
        });

        _sTestInteractionSystem.ClearHandlers();
    }


    [Test]
    public async Task InteractionOutOfRangeTest()
    {
        await Pair.CreateTestMap();
        Assert.That(TestMap, Is.Not.Null);
        var coords = TestMap.GridCoords;

        EntityUid user = default;
        EntityUid target = default;
        EntityUid item = default;

        await Server.WaitAssertion(() =>
        {
            user = SSpawnAtPosition(null, coords);
            SEntMan.EnsureComponent<HandsComponent>(user);
            _sHandsSystem.AddHand(user, "hand", HandLocation.Left);
            target = SSpawnAtPosition(null, new EntityCoordinates(TestMap.MapUid, new Vector2(SharedInteractionSystem.InteractionRange + 0.01f, 0)));
            item = SSpawnAtPosition(null, coords);
            SEntMan.EnsureComponent<ItemComponent>(item);
        });

        await Server.WaitRunTicks(1);

        var interactUsing = false;
        var interactHand = false;
        await Server.WaitAssertion(() =>
        {
            _sTestInteractionSystem.InteractUsingEvent = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactUsing = true; };
            _sTestInteractionSystem.InteractHandEvent = (ev) => { Assert.That(ev.Target, Is.EqualTo(target)); interactHand = true; };

            _sInteractionSystem.UserInteraction(user, SComp<TransformComponent>(target).Coordinates, target);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand, Is.False);
            }

            Assert.That(_sHandsSystem.TryPickup(user, item));

            _sInteractionSystem.UserInteraction(user, SComp<TransformComponent>(target).Coordinates, target);
            Assert.That(interactUsing, Is.False);
        });

        _sTestInteractionSystem.ClearHandlers();
    }

    [Test]
    public async Task InsideContainerInteractionBlockTest()
    {
        await Pair.CreateTestMap();
        Assert.That(TestMap, Is.Not.Null);
        var coords = TestMap.GridCoords;

        EntityUid user = default;
        EntityUid target = default;
        EntityUid item = default;
        EntityUid containerEntity = default;
        BaseContainer container = null!;

        await Server.WaitAssertion(() =>
        {
            user = SSpawnAtPosition(null, coords);
            SEntMan.EnsureComponent<HandsComponent>(user);
            SEntMan.EnsureComponent<ComplexInteractionComponent>(user);
            _sHandsSystem.AddHand(user, "hand", HandLocation.Left);
            target = SSpawnAtPosition(null, coords);
            item = SSpawnAtPosition(null, coords);
            SEntMan.EnsureComponent<ItemComponent>(item);
            containerEntity = SSpawnAtPosition(null, coords);
            container = _sContainerSystem.EnsureContainer<Container>(containerEntity, "InteractionTestContainer");
        });

        await Server.WaitRunTicks(1);

        var interactUsing = false;
        var interactHand = false;
        await Server.WaitAssertion(() =>
        {
#pragma warning disable NUnit2045 // Interdependent assertions.
            Assert.That(_sContainerSystem.Insert(user, container));
            Assert.That(SComp<TransformComponent>(user).ParentUid, Is.EqualTo(containerEntity));
#pragma warning restore NUnit2045

            _sTestInteractionSystem.InteractUsingEvent = (ev) => { Assert.That(ev.Target, Is.EqualTo(containerEntity)); interactUsing = true; };
            _sTestInteractionSystem.InteractHandEvent = (ev) => { Assert.That(ev.Target, Is.EqualTo(containerEntity)); interactHand = true; };

            _sInteractionSystem.UserInteraction(user, SComp<TransformComponent>(target).Coordinates, target);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand, Is.False);
            }

            _sInteractionSystem.UserInteraction(user, SComp<TransformComponent>(containerEntity).Coordinates, containerEntity);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(interactUsing, Is.False);
                Assert.That(interactHand, Is.True);
            }

            Assert.That(_sHandsSystem.TryPickup(user, item));

            _sInteractionSystem.UserInteraction(user, SComp<TransformComponent>(target).Coordinates, target);
            Assert.That(interactUsing, Is.False);

            _sInteractionSystem.UserInteraction(user, SComp<TransformComponent>(containerEntity).Coordinates, containerEntity);
            Assert.That(interactUsing, Is.True);
        });

        _sTestInteractionSystem.ClearHandlers();
    }

    public sealed partial class TestInteractionSystem : EntitySystem
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

using Content.Server.MachineLinking.Events;
using Content.Server.MachineLinking.System;
using Content.Server.Recycling;
using Content.Server.Recycling.Components;
using Content.Shared.Conveyor;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Physics.Controllers;
using Content.Shared.Power.Events;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;

namespace Content.Server.Physics.Controllers;

public sealed class ConveyorController : SharedConveyorController
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly RecyclerSystem _recycler = default!;
    [Dependency] private readonly SignalLinkerSystem _signalSystem = default!;
    [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        UpdatesAfter.Add(typeof(MoverController));
        SubscribeLocalEvent<ConveyorComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ConveyorComponent, ComponentShutdown>(OnConveyorShutdown);
        SubscribeLocalEvent<ConveyorComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<ConveyorComponent, PowerChangedEvent>(OnPowerChanged);

        base.Initialize();
    }

    private void OnInit(EntityUid uid, ConveyorComponent component, ComponentInit args)
    {
        _signalSystem.EnsureReceiverPorts(uid, component.ReversePort, component.ForwardPort, component.OffPort);

        if (TryComp<PhysicsComponent>(uid, out var body))
        {
            var shape = new PolygonShape();
            shape.SetAsBox(0.55f, 0.55f);

            _fixtures.TryCreateFixture(body, new Fixture(body, shape)
            {
                ID = ConveyorFixture,
                CollisionLayer = (int) (CollisionGroup.LowImpassable | CollisionGroup.MidImpassable | CollisionGroup.Impassable),
                Hard = false,
            });
        }
    }

    private void OnConveyorShutdown(EntityUid uid, ConveyorComponent component, ComponentShutdown args)
    {
        if (MetaData(uid).EntityLifeStage >= EntityLifeStage.Terminating)
            return;

        RemComp<ActiveConveyorComponent>(uid);

        if (!TryComp<PhysicsComponent>(uid, out var body))
            return;

        _fixtures.DestroyFixture(body, ConveyorFixture);
    }

    private void OnPowerChanged(EntityUid uid, ConveyorComponent component, ref PowerChangedEvent args)
    {
        component.Powered = args.Powered;
        UpdateAppearance(component);
    }

    private void UpdateAppearance(ConveyorComponent component)
    {
        _appearance.SetData(component.Owner, ConveyorVisuals.State, component.Powered ? component.State : ConveyorState.Off);
    }

    private void OnSignalReceived(EntityUid uid, ConveyorComponent component, SignalReceivedEvent args)
    {
        if (args.Port == component.OffPort)
            SetState(uid, ConveyorState.Off, component);

        else if (args.Port == component.ForwardPort)
        {
            AwakenEntities(component);
            SetState(uid, ConveyorState.Forward, component);
        }

        else if (args.Port == component.ReversePort)
        {
            AwakenEntities(component);
            SetState(uid, ConveyorState.Reverse, component);
        }
    }

    private void SetState(EntityUid uid, ConveyorState state, ConveyorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.State = state;

        if (TryComp<PhysicsComponent>(uid, out var physics))
            _broadphase.RegenerateContacts(physics);

        if (TryComp<RecyclerComponent>(component.Owner, out var recycler))
        {
            if (component.State != ConveyorState.Off)
                _recycler.EnableRecycler(recycler);
            else
                _recycler.DisableRecycler(recycler);
        }

        UpdateAppearance(component);
    }

    /// <summary>
    /// Awakens sleeping entities on the conveyor belt's tile when it's turned on.
    /// Fixes an issue where non-hard/sleeping entities refuse to wake up + collide if a belt is turned off and on again.
    /// </summary>
    private void AwakenEntities(ConveyorComponent component)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();
        var bodyQuery = GetEntityQuery<PhysicsComponent>();

        if (!xformQuery.TryGetComponent(component.Owner, out var xform))
            return;

        var beltTileRef = xform.Coordinates.GetTileRef(EntityManager, _mapManager);

        if (beltTileRef != null)
        {
            var intersecting = _lookup.GetEntitiesIntersecting(beltTileRef.Value);

            foreach (var entity in intersecting)
            {
                if (!bodyQuery.TryGetComponent(entity, out var physics))
                    continue;

                if (physics.BodyType != BodyType.Static)
                    _physics.WakeBody(physics);
            }
        }
    }
}

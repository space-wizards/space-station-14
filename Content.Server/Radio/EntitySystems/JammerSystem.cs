using System.Linq;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Physics;
using Content.Shared.Radio.Components;
using Content.Shared.Radio.EntitySystems;
using Content.Shared.Silicons.StationAi;
using Content.Shared.StationAi;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Radio.EntitySystems;

public sealed class JammerSystem : SharedJammerSystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedDeviceNetworkJammerSystem _jammer = default!;
    [Dependency] private readonly FixtureSystem _fixture = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadioSendAttemptEvent>(OnRadioSendAttempt);
        SubscribeLocalEvent<RadioReceiveAttemptEvent>(OnRadioReceiveAttempt);
        SubscribeLocalEvent<RadioJammerFixtureComponent, StartCollideEvent>(OnJammerStartCollide);
        SubscribeLocalEvent<RadioJammerFixtureComponent, EndCollideEvent>(OnJammerEndCollide);
        SubscribeLocalEvent<RadioJammerFixtureComponent, ComponentShutdown>(OnFixtureShutdown);
        SubscribeLocalEvent<RadioJammerFixtureComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<RadioJammerComponent, RadioJammerPowerLevelChangedEvent>(OnPowerLevelChanged);
    }

    protected override void OnItemToggle(Entity<RadioJammerComponent> ent, ref ItemToggledEvent args)
    {
        base.OnItemToggle(ent, ref args);

        if (args.Activated)
        {
            var fixtureComp = EnsureComp<RadioJammerFixtureComponent>(ent);
            CreateJammerFixture(ent, GetCurrentRange(ent), fixtureComp);

            if (TryComp<TransformComponent>(ent, out var xform))
            {
                UpdateJammedCameras((ent.Owner, fixtureComp, ent.Comp, xform));
                fixtureComp.LastPosition = _transform.GetMapCoordinates(ent.Owner, xform);
                fixtureComp.NextUpdateTime = _timing.CurTime + TimeSpan.FromSeconds(0.1);
            }
        }
        else
        {
            if (TryComp<RadioJammerFixtureComponent>(ent, out var fixtureComp))
            {
                DestroyJammerFixture(ent, fixtureComp);
                RemComp<RadioJammerFixtureComponent>(ent);
            }
        }
    }

    private void OnRadioSendAttempt(ref RadioSendAttemptEvent args)
    {
        if (ShouldCancel(args.RadioSource, args.Channel.Frequency))
            args.Cancelled = true;
    }

    private void OnRadioReceiveAttempt(ref RadioReceiveAttemptEvent args)
    {
        if (ShouldCancel(args.RadioReceiver, args.Channel.Frequency))
            args.Cancelled = true;
    }

    private bool ShouldCancel(EntityUid sourceUid, int frequency)
    {
        var query = EntityQueryEnumerator<ActiveRadioJammerComponent, RadioJammerComponent, RadioJammerFixtureComponent>();

        while (query.MoveNext(out _, out _, out var jam, out var fixture))
        {
            if (jam.FrequenciesExcluded.Contains(frequency))
                continue;

            if (fixture.CollidingEntities.ContainsKey(sourceUid))
                return true;
        }

        return false;
    }

    private void CreateJammerFixture(
        Entity<RadioJammerComponent> ent,
        float range,
        RadioJammerFixtureComponent fixtureComp)
    {
        if (!TryComp<PhysicsComponent>(ent, out var body))
            return;

        var shape = new PhysShapeCircle(range);

        _fixture.TryCreateFixture(
            ent.Owner,
            shape,
            RadioJammerFixtureComponent.FixtureID,
            hard: false,
            body: body,
            collisionLayer: (int)CollisionGroup.Opaque,
            collisionMask: (int)CollisionGroup.Opaque);

        _physics.RegenerateContacts((ent.Owner, body));
    }

    private void DestroyJammerFixture(
        EntityUid uid,
        RadioJammerFixtureComponent fixtureComp)
    {
        if (!TryComp<FixturesComponent>(uid, out var fixtures))
            return;

        if (fixtures.Fixtures.TryGetValue(RadioJammerFixtureComponent.FixtureID, out var fixture))
            _fixture.DestroyFixture(uid, RadioJammerFixtureComponent.FixtureID, fixture, manager: fixtures);

        foreach (var camera in fixtureComp.JammedCameras.ToList())
        {
            if (TryComp<AiCameraJammedComponent>(camera, out var jammedComp))
            {
                jammedComp.JammingSources.Remove(uid);
                if (jammedComp.JammingSources.Count == 0)
                    RemComp<AiCameraJammedComponent>(camera);
                else
                    Dirty(camera, jammedComp);
            }
        }

        fixtureComp.CollidingEntities.Clear();
        fixtureComp.JammedCameras.Clear();
    }

    private void OnJammerStartCollide(
        Entity<RadioJammerFixtureComponent> ent,
        ref StartCollideEvent args)
    {
        if (args.OurFixtureId != RadioJammerFixtureComponent.FixtureID)
            return;

        ent.Comp.CollidingEntities[args.OtherEntity] = args.OtherBody;

        if (HasComp<StationAiVisionComponent>(args.OtherEntity))
        {
            ent.Comp.JammedCameras.Add(args.OtherEntity);
            var jammedComp = EnsureComp<AiCameraJammedComponent>(args.OtherEntity);
            jammedComp.JammingSources.Add(ent.Owner);
            Dirty(args.OtherEntity, jammedComp);
        }
    }

    private void OnJammerEndCollide(
        Entity<RadioJammerFixtureComponent> ent,
        ref EndCollideEvent args)
    {
        if (args.OurFixtureId != RadioJammerFixtureComponent.FixtureID)
            return;

        ent.Comp.CollidingEntities.Remove(args.OtherEntity);

        if (ent.Comp.JammedCameras.Remove(args.OtherEntity))
        {
            if (TryComp<AiCameraJammedComponent>(args.OtherEntity, out var jammedComp))
            {
                jammedComp.JammingSources.Remove(ent.Owner);

                if (jammedComp.JammingSources.Count == 0)
                    RemComp<AiCameraJammedComponent>(args.OtherEntity);
                else
                    Dirty(args.OtherEntity, jammedComp);
            }
        }
    }

    private void OnFixtureShutdown(Entity<RadioJammerFixtureComponent> ent, ref ComponentShutdown args)
    {
        DestroyJammerFixture(ent.Owner, ent.Comp);
    }

    private void OnParentChanged(Entity<RadioJammerFixtureComponent> ent, ref EntParentChangedMessage args)
    {
        if (!TryComp<RadioJammerComponent>(ent, out var jammer) ||
            !TryComp<TransformComponent>(ent, out var xform))
            return;

        UpdateJammedCameras((ent.Owner, ent.Comp, jammer, xform));
        ent.Comp.LastPosition = _transform.GetMapCoordinates(ent.Owner, xform);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<RadioJammerFixtureComponent, RadioJammerComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var fixture, out var jammer, out var xform))
        {
            if (curTime < fixture.NextUpdateTime)
                continue;

            var currentPos = _transform.GetMapCoordinates(uid, xform);

            if (fixture.LastPosition == null ||
                fixture.LastPosition.Value.MapId != currentPos.MapId ||
                !fixture.LastPosition.Value.Position.EqualsApprox(currentPos.Position, 0.01f))
            {
                UpdateJammedCameras((uid, fixture, jammer, xform));
                fixture.LastPosition = currentPos;
            }

            fixture.NextUpdateTime = curTime + TimeSpan.FromSeconds(0.1);
        }
    }

    /// <summary>
    /// Updates which cameras are jammed based on current position using EntityLookup.
    /// This handles the case when the jammer is held/carried and collision doesn't fire.
    /// </summary>
    private void UpdateJammedCameras(Entity<RadioJammerFixtureComponent, RadioJammerComponent, TransformComponent> jammer)
    {
        var range = GetCurrentRange((jammer.Owner, jammer.Comp2));
        var jammerPos = _transform.GetMapCoordinates(jammer.Owner, jammer.Comp3);

        var camerasInRange = new HashSet<EntityUid>();
        foreach (var camera in _lookup.GetEntitiesInRange<StationAiVisionComponent>(jammerPos, range))
        {
            camerasInRange.Add(camera.Owner);

            if (!jammer.Comp1.JammedCameras.Contains(camera.Owner))
            {
                var jammedComp = EnsureComp<AiCameraJammedComponent>(camera);
                jammedComp.JammingSources.Add(jammer.Owner);
                Dirty(camera.Owner, jammedComp);
            }
        }

        var camerasToRestore = new List<EntityUid>();
        foreach (var previousCamera in jammer.Comp1.JammedCameras)
        {
            if (!camerasInRange.Contains(previousCamera))
                camerasToRestore.Add(previousCamera);
        }

        foreach (var camera in camerasToRestore)
        {
            if (TryComp<AiCameraJammedComponent>(camera, out var jammedComp))
            {
                jammedComp.JammingSources.Remove(jammer.Owner);

                if (jammedComp.JammingSources.Count == 0)
                    RemComp<AiCameraJammedComponent>(camera);
                else
                    Dirty(camera, jammedComp);
            }
        }

        jammer.Comp1.JammedCameras.Clear();
        foreach (var camera in camerasInRange)
            jammer.Comp1.JammedCameras.Add(camera);
    }

    private void OnPowerLevelChanged(Entity<RadioJammerComponent> ent, ref RadioJammerPowerLevelChangedEvent args)
    {
        if (!TryComp<RadioJammerFixtureComponent>(ent, out var fixtureComp))
            return;

        // Recreate fixture with updated range
        DestroyJammerFixture(ent, fixtureComp);
        CreateJammerFixture(ent, GetCurrentRange(ent), fixtureComp);

        if (TryComp<TransformComponent>(ent, out var xform))
        {
            UpdateJammedCameras((ent.Owner, fixtureComp, ent.Comp, xform));
            fixtureComp.LastPosition = _transform.GetMapCoordinates(ent.Owner, xform);
        }

        if (TryComp<DeviceNetworkJammerComponent>(ent, out var jammingComp))
            _jammer.SetRange((ent, jammingComp), GetCurrentRange(ent));
    }
}

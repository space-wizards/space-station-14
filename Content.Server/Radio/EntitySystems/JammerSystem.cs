using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Radio.Components;
using Content.Shared.Radio.EntitySystems;
using Content.Shared.Silicons.StationAi;
using Content.Shared.StationAi;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Radio.EntitySystems;

public sealed class JammerSystem : SharedJammerSystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedDeviceNetworkJammerSystem _jammer = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadioSendAttemptEvent>(OnRadioSendAttempt);
        SubscribeLocalEvent<RadioReceiveAttemptEvent>(OnRadioReceiveAttempt);
        SubscribeLocalEvent<RadioJammerFixtureComponent, ComponentShutdown>(OnTrackerShutdown);
        SubscribeLocalEvent<RadioJammerFixtureComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<RadioJammerComponent, RadioJammerPowerLevelChangedEvent>(OnPowerLevelChanged);
    }

    protected override void OnItemToggle(Entity<RadioJammerComponent> ent, ref ItemToggledEvent args)
    {
        base.OnItemToggle(ent, ref args);

        if (args.Activated)
        {
            var tracker = EnsureComp<RadioJammerFixtureComponent>(ent);
            var xform = Transform(ent.Owner);
            UpdateJammedCameras((ent.Owner, tracker, ent.Comp, xform));
            tracker.LastPosition = _transform.GetMapCoordinates(ent.Owner, xform);
            tracker.NextUpdateTime = _timing.CurTime + TimeSpan.FromSeconds(0.1);
        }
        else
        {
            if (TryComp<RadioJammerFixtureComponent>(ent, out var tracker))
            {
                CleanupJammedCameras(ent.Owner, tracker);
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
        var sourcePos = _transform.GetMapCoordinates(sourceUid);
        if (sourcePos.MapId == MapId.Nullspace)
            return false;

        var query = EntityQueryEnumerator<ActiveRadioJammerComponent, RadioJammerComponent>();
        while (query.MoveNext(out var jammerUid, out _, out var jam))
        {
            if (jam.FrequenciesExcluded.Contains(frequency))
                continue;

            var jammerPos = _transform.GetMapCoordinates(jammerUid);
            if (jammerPos.MapId != sourcePos.MapId)
                continue;

            var range = GetCurrentRange((jammerUid, jam));
            if ((jammerPos.Position - sourcePos.Position).LengthSquared() <= range * range)
                return true;
        }

        return false;
    }

    private void CleanupJammedCameras(EntityUid uid, RadioJammerFixtureComponent tracker)
    {
        foreach (var camera in tracker.JammedCameras)
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
        tracker.JammedCameras.Clear();
    }

    private void OnTrackerShutdown(Entity<RadioJammerFixtureComponent> ent, ref ComponentShutdown _)
    {
        CleanupJammedCameras(ent.Owner, ent.Comp);
    }

    private void OnParentChanged(Entity<RadioJammerFixtureComponent> ent, ref EntParentChangedMessage args)
    {
        if (!TryComp<RadioJammerComponent>(ent, out var jammer))
            return;

        var xform = Transform(ent.Owner);
        UpdateJammedCameras((ent.Owner, ent.Comp, jammer, xform));
        ent.Comp.LastPosition = _transform.GetMapCoordinates(ent.Owner, xform);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<RadioJammerFixtureComponent, RadioJammerComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var tracker, out var jammer, out var xform))
        {
            if (curTime < tracker.NextUpdateTime)
                continue;

            var currentPos = _transform.GetMapCoordinates(uid, xform);

            if (tracker.LastPosition == null ||
                tracker.LastPosition.Value.MapId != currentPos.MapId ||
                !tracker.LastPosition.Value.Position.EqualsApprox(currentPos.Position, 0.01f))
            {
                UpdateJammedCameras((uid, tracker, jammer, xform));
                tracker.LastPosition = currentPos;
            }

            tracker.NextUpdateTime = curTime + TimeSpan.FromSeconds(0.1);
        }
    }

    /// <summary>
    /// Updates which cameras are jammed based on current position using EntityLookup.
    /// Works regardless of whether the jammer is on the floor, held, or inside a container.
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
        if (!TryComp<RadioJammerFixtureComponent>(ent, out var tracker))
            return;

        var xform = Transform(ent.Owner);
        UpdateJammedCameras((ent.Owner, tracker, ent.Comp, xform));
        tracker.LastPosition = _transform.GetMapCoordinates(ent.Owner, xform);

        if (TryComp<DeviceNetworkJammerComponent>(ent, out var jammingComp))
            _jammer.SetRange((ent, jammingComp), GetCurrentRange(ent));
    }
}

using Content.Server.Anomaly.Components;
using Content.Server.Beam;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.Anomaly.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.Emag.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Anomaly.Effects;

public sealed class TechAnomalySystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _signal = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly BeamSystem _beam = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EmagSystem _emag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TechAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<TechAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
        SubscribeLocalEvent<TechAnomalyComponent, AnomalyStabilityChangedEvent>(OnStabilityChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TechAnomalyComponent, AnomalyComponent>();
        while (query.MoveNext(out var uid, out var tech, out var anom))
        {
            if (_timing.CurTime < tech.NextTimer)
                continue;

            tech.NextTimer += TimeSpan.FromSeconds(tech.TimerFrequency);

            _signal.InvokePort(uid, tech.TimerPort);
        }
    }

    private void OnStabilityChanged(Entity<TechAnomalyComponent> tech, ref AnomalyStabilityChangedEvent args)
    {
        var links = MathHelper.Lerp(tech.Comp.LinkCountPerPulse.Min, tech.Comp.LinkCountPerPulse.Max, args.Severity);
        CreateNewRandomLink(tech, (int)links);
    }

    private void CreateNewRandomLink(Entity<TechAnomalyComponent> tech, int count)
    {
        if (!TryComp<AnomalyComponent>(tech, out var anomaly))
            return;
        if (!TryComp<DeviceLinkSourceComponent>(tech, out var sourceComp))
            return;

        var range = MathHelper.Lerp(tech.Comp.LinkRadius.Min, tech.Comp.LinkRadius.Max, anomaly.Severity);

        var devices = _lookup.GetEntitiesInRange<DeviceLinkSinkComponent>(Transform(tech).Coordinates, range);
        if (devices.Count < 1)
            return;

        for (var i = 0; i < count; i++)
        {
            var device = _random.Pick(devices);
            CreateNewLink(tech, (tech, sourceComp), device);
        }
    }

    private void CreateNewLink(Entity<TechAnomalyComponent> tech, Entity<DeviceLinkSourceComponent> source, Entity<DeviceLinkSinkComponent> target)
    {
        var sourcePort = _random.Pick(source.Comp.Ports);
        var sinkPort = _random.Pick(target.Comp.Ports);

        _signal.SaveLinks(null, source, target,new()
        {
            (sourcePort, sinkPort),
        });
        _beam.TryCreateBeam(source, target, tech.Comp.LinkBeamProto);
    }

    private void OnSupercritical(Entity<TechAnomalyComponent> tech, ref AnomalySupercriticalEvent args)
    {
        // We remove the component so that the anomaly does not bind itself to other devices before self destroy.
        RemComp<DeviceLinkSourceComponent>(tech);

        var sources =
            _lookup.GetEntitiesInRange<DeviceLinkSourceComponent>(Transform(tech).Coordinates,
                tech.Comp.LinkRadius.Max);

        var sinks =
            _lookup.GetEntitiesInRange<DeviceLinkSinkComponent>(Transform(tech).Coordinates,
                tech.Comp.LinkRadius.Max);

        for (var i = 0; i < tech.Comp.LinkCountSupercritical; i++)
        {
            if (sources.Count < 1)
                return;

            if (sinks.Count < 1)
                return;

            var source = _random.Pick(sources);
            sources.Remove(source);

            var sink = _random.Pick(sinks);
            sinks.Remove(sink);

            if (_random.Prob(tech.Comp.EmagSupercritProbability))
            {
                _emag.DoEmagEffect(tech, source);
                _emag.DoEmagEffect(tech, sink);
            }

            CreateNewLink(tech, source, sink);
        }
    }

    private void OnPulse(Entity<TechAnomalyComponent> tech, ref AnomalyPulseEvent args)
    {
        _signal.InvokePort(tech, tech.Comp.PulsePort);
    }
}

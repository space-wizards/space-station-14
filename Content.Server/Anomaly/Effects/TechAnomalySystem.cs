using Content.Server.Anomaly.Components;
using Content.Server.Beam;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.Anomaly.Components;
using Content.Shared.DeviceLinking;
using Robust.Shared.Random;

namespace Content.Server.Anomaly.Effects;

public sealed class TechAnomalySystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _signal = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly BeamSystem _beam = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TechAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<TechAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
        SubscribeLocalEvent<TechAnomalyComponent, AnomalyStabilityChangedEvent>(OnStabilityChanged);
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

        var range = MathHelper.Lerp(tech.Comp.LinkRadius.Min, tech.Comp.LinkRadius.Max, anomaly.Severity);

        var devices = _lookup.GetEntitiesInRange<DeviceLinkSinkComponent>(Transform(tech).Coordinates, range);
        for (var i = 0; i < count; i++)
        {
            var device = _random.Pick(devices);
            CreateNewLink(tech, device);
        }
    }

    private void CreateNewLink(Entity<TechAnomalyComponent> tech, Entity<DeviceLinkSinkComponent> target)
    {
        var port = _random.Pick(target.Comp.Ports);
        _signal.SaveLinks(null, tech, target,new()
        {
            (tech.Comp.PulsePort, port),
        });
        _beam.TryCreateBeam(tech, target, tech.Comp.LinkBeamProto);
    }

    private void OnSupercritical(Entity<TechAnomalyComponent> tech, ref AnomalySupercriticalEvent args)
    {
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


            var sourcePort = _random.Pick(source.Comp.Ports);
            var sinkPort = _random.Pick(sink.Comp.Ports);

            _signal.SaveLinks(null, source, sink,new()
            {
                (sourcePort, sinkPort),
            });

            _beam.TryCreateBeam(source, sink, tech.Comp.LinkBeamProto);
        }
    }

    private void OnPulse(Entity<TechAnomalyComponent> anomaly, ref AnomalyPulseEvent args)
    {
        _signal.InvokePort(anomaly, anomaly.Comp.PulsePort);
    }
}

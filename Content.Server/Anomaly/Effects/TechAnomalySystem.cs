using Content.Server.Anomaly.Components;
using Content.Server.Beam;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.Anomaly.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.Emag.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Anomaly.Effects;

public sealed partial class TechAnomalySystem : EntitySystem
{
    private static readonly EntityTimerId SignalTimer = new("signal");

    [Dependency] private DeviceLinkSystem _signal = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private BeamSystem _beam = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityTimerSystem _timers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TechAnomalyComponent, MapInitEvent>(OnTechMapInit);
        SubscribeLocalEvent<TechAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<TechAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
        SubscribeLocalEvent<TechAnomalyComponent, AnomalyStabilityChangedEvent>(OnStabilityChanged);
        SubscribeLocalEvent<TechAnomalyComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnTechMapInit(Entity<TechAnomalyComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextTimer = _timing.CurTime;
        _timers.SetTimerAt(ent, SignalTimer, ent.Comp.NextTimer, TimeSpan.FromSeconds(ent.Comp.TimerFrequency));
    }

    private void OnTimer(Entity<TechAnomalyComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != SignalTimer)
            return;

        ent.Comp.NextTimer = args.NextDeadline ?? args.FiredAt;
        _signal.InvokePort(ent, ent.Comp.TimerPort);
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
                var sourceEv = new GotEmaggedEvent(tech, EmagType.Access | EmagType.Interaction);
                RaiseLocalEvent(source, ref sourceEv);

                var sinkEv = new GotEmaggedEvent(tech, EmagType.Access | EmagType.Interaction);
                RaiseLocalEvent(sink, ref sinkEv);
            }

            CreateNewLink(tech, source, sink);
        }
    }

    private void OnPulse(Entity<TechAnomalyComponent> tech, ref AnomalyPulseEvent args)
    {
        _signal.InvokePort(tech, tech.Comp.PulsePort);
    }
}

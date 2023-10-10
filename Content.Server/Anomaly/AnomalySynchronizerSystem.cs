using Content.Server.Anomaly.Components;
using Content.Server.Construction;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Research.Components;

namespace Content.Server.Anomaly;

/// <summary>
/// This handles anomalous vessel as well as
/// the calculations for how many points they
/// should produce.
/// </summary>
public sealed partial class AnomalySynchronizerSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
    [Dependency] private readonly AnomalySystem _anomaly = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnomalySynchronizerComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<AnomalySynchronizerComponent, PowerChangedEvent>(OnPowerChanged);

        SubscribeLocalEvent<AnomalyPulseEvent>(OnAnomalyPulse);
        SubscribeLocalEvent<AnomalySupercriticalEvent>(OnAnomalySupercritical);

        SubscribeLocalEvent<AnomalyStabilityChangedEvent>(OnAnomalyStabilityChanged);
    }

    private void OnPowerChanged(EntityUid uid, AnomalySynchronizerComponent component, ref PowerChangedEvent args)
    {
        if (args.Powered)
            return;

        if (!TryComp<AnomalyComponent>(component.ConnectedAnomaly, out var anomaly))
            return;

        _anomaly.DoAnomalyPulse(component.ConnectedAnomaly, anomaly);
    }

    private void OnInteractHand(EntityUid uid, AnomalySynchronizerComponent component, InteractHandEvent args)
    {
        foreach (var entity in _entityLookup.GetEntitiesInRange(uid, 0.15f))
        {
            if (TryComp<AnomalyComponent>(entity, out var anomaly))
            {
                ConnectToAnomaly(uid, component, entity);
                break;
            }
        }
    }

    private void ConnectToAnomaly(EntityUid uid, AnomalySynchronizerComponent component, EntityUid auid)
    {
        if (!TryComp<AnomalyComponent>(auid, out var anomaly))
            return;

        component.ConnectedAnomaly = auid;
        //move the anomaly to the center of the synchronizer, for aesthetics.
        var targetXform = _transform.GetWorldPosition(uid);
        _transform.SetWorldPosition(auid, targetXform);
    }

    private void DisconnetFromAnomaly(EntityUid uid, AnomalySynchronizerComponent component)
    {
        
    }

    private void OnAnomalyPulse(ref AnomalyPulseEvent args)
    {
        var query = EntityQueryEnumerator<AnomalySynchronizerComponent>();
        while (query.MoveNext(out var ent, out var component))
        {
            if (args.Anomaly != component.ConnectedAnomaly)
                continue;

            var uid = component.Owner;

            if (!TryComp<ApcPowerReceiverComponent>(uid, out var apcPower))
                continue;

            if (!apcPower.Powered)
                continue;

            _signalSystem.InvokePort(uid, component.PulsePort);
        }
    }

    private void OnAnomalySupercritical(ref AnomalySupercriticalEvent args)
    {
        var query = EntityQueryEnumerator<AnomalySynchronizerComponent>();
        while (query.MoveNext(out var ent, out var component))
        {
            if (args.Anomaly != component.ConnectedAnomaly)
                continue;

            var uid = component.Owner;

            if (!TryComp<ApcPowerReceiverComponent>(uid, out var apcPower))
                continue;

            if (!apcPower.Powered)
                continue;

            _signalSystem.InvokePort(uid, component.SupercritPort);
        }
    }
    private void OnAnomalyStabilityChanged(ref AnomalyStabilityChangedEvent args)
    {
        var query = EntityQueryEnumerator<AnomalySynchronizerComponent>();
        while (query.MoveNext(out var ent, out var component))
        {
            if (args.Anomaly != component.ConnectedAnomaly)
                continue;

            var uid = component.Owner;

            if (!TryComp<ApcPowerReceiverComponent>(uid, out var apcPower))
                continue;

            if (!apcPower.Powered)
                continue;

            if (args.Stability < 0.25f) //I couldn't find where these values are stored, so I hardcoded them. I should change that.
            {
                _signalSystem.InvokePort(component.Owner, component.DecayingPort);
            }
            else if (args.Stability > 0.5f) //I couldn't find where these values are stored, so I hardcoded them. I should change that.
            {
                _signalSystem.InvokePort(component.Owner, component.GrowingPort);
            }
            else
            {
                _signalSystem.InvokePort(component.Owner, component.NormalizePort);
            }
        }
    }
}

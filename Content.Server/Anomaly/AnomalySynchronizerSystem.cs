using System.Linq;
using Content.Server.Anomaly.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Anomaly.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Content.Shared.Verbs;

namespace Content.Server.Anomaly;

/// <summary>
/// a device that allows you to translate anomaly activity into multitool signals.
/// </summary>
public sealed partial class AnomalySynchronizerSystem : EntitySystem
{
    [Dependency] private readonly AnomalySystem _anomaly = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly DeviceLinkSystem _signal = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnomalySynchronizerComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<AnomalySynchronizerComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<AnomalySynchronizerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<AnomalySynchronizerComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);

        SubscribeLocalEvent<AnomalyPulseEvent>(OnAnomalyPulse);
        SubscribeLocalEvent<AnomalySeverityChangedEvent>(OnAnomalySeverityChanged);
        SubscribeLocalEvent<AnomalyStabilityChangedEvent>(OnAnomalyStabilityChanged);
    }

    /// <summary>
    /// If powered, try to attach a nearby anomaly.
    /// </summary>
    public bool TryAttachNearbyAnomaly(Entity<AnomalySynchronizerComponent> ent, EntityUid? user = null)
    {
        if (!_power.IsPowered(ent))
        {
            if (user is not null)
                _popup.PopupEntity(Loc.GetString("base-computer-ui-component-not-powered", ("machine", ent)), ent, user.Value);

            return false;
        }

        var coords = _transform.GetMapCoordinates(ent);
        var anomaly = _entityLookup.GetEntitiesInRange<AnomalyComponent>(coords, ent.Comp.AttachRange).FirstOrDefault();

        if (anomaly.Owner is { Valid: false }) // no anomaly in range
        {
            if (user is not null)
                _popup.PopupEntity(Loc.GetString("anomaly-sync-no-anomaly"), ent, user.Value);

            return false;
        }

        ConnectToAnomaly(ent, anomaly);
        return true;
    }

    private void OnPowerChanged(Entity<AnomalySynchronizerComponent> ent, ref PowerChangedEvent args)
    {
        if (args.Powered)
            return;

        if (!TryComp<AnomalyComponent>(ent.Comp.ConnectedAnomaly, out var anomaly))
            return;

        DisconneсtFromAnomaly(ent, anomaly);
    }

    private void OnExamined(Entity<AnomalySynchronizerComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(ent.Comp.ConnectedAnomaly.HasValue ? "anomaly-sync-examine-connected" : "anomaly-sync-examine-not-connected"));
    }

    private void OnGetInteractionVerbs(Entity<AnomalySynchronizerComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands is null || ent.Comp.ConnectedAnomaly.HasValue)
            return;

        var user = args.User;
        args.Verbs.Add(new()
        {
            Act = () =>
            {
                TryAttachNearbyAnomaly(ent, user);
            },
            Message = Loc.GetString("anomaly-sync-connect-verb-message", ("machine", ent)),
            Text = Loc.GetString("anomaly-sync-connect-verb-text"),
        });
    }

    private void OnInteractHand(Entity<AnomalySynchronizerComponent> ent, ref InteractHandEvent args)
    {
        TryAttachNearbyAnomaly(ent, args.User);
    }

    private void ConnectToAnomaly(Entity<AnomalySynchronizerComponent> ent, Entity<AnomalyComponent> anomaly)
    {
        if (ent.Comp.ConnectedAnomaly == anomaly)
            return;

        ent.Comp.ConnectedAnomaly = anomaly;
        //move the anomaly to the center of the synchronizer, for aesthetics.
        var targetXform = _transform.GetWorldPosition(ent);
        _transform.SetWorldPosition(anomaly, targetXform);

        if (ent.Comp.PulseOnConnect)
            _anomaly.DoAnomalyPulse(anomaly, anomaly);

        _popup.PopupEntity(Loc.GetString("anomaly-sync-connected"), ent, PopupType.Medium);
        _audio.PlayPvs(ent.Comp.ConnectedSound, ent);
    }

    //TO DO: disconnection from the anomaly should also be triggered if the anomaly is far away from the synchronizer.
    //Currently only bluespace anomaly can do this, but for some reason it is the only one that cannot be connected to the synchronizer.
    private void DisconneсtFromAnomaly(Entity<AnomalySynchronizerComponent> ent, AnomalyComponent anomaly)
    {
        if (ent.Comp.ConnectedAnomaly == null)
            return;

        if (ent.Comp.PulseOnDisconnect)
            _anomaly.DoAnomalyPulse(ent.Comp.ConnectedAnomaly.Value, anomaly);

        _popup.PopupEntity(Loc.GetString("anomaly-sync-disconnected"), ent, PopupType.Large);
        _audio.PlayPvs(ent.Comp.ConnectedSound, ent);

        ent.Comp.ConnectedAnomaly = null;
    }

    private void OnAnomalyPulse(ref AnomalyPulseEvent args)
    {
        var query = EntityQueryEnumerator<AnomalySynchronizerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (args.Anomaly != component.ConnectedAnomaly)
                continue;

            if (!_power.IsPowered(uid))
                continue;

            _signal.InvokePort(uid, component.PulsePort);
        }
    }

    private void OnAnomalySeverityChanged(ref AnomalySeverityChangedEvent args)
    {
        var query = EntityQueryEnumerator<AnomalySynchronizerComponent>();
        while (query.MoveNext(out var ent, out var component))
        {
            if (args.Anomaly != component.ConnectedAnomaly)
                continue;

            if (!_power.IsPowered(ent))
                continue;

            //The superscritical port is invoked not at the AnomalySupercriticalEvent,
            //but at the moment the growth animation starts. Otherwise, there is no point in this port.
            //ATTENTION! the console command supercriticalanomaly does not work here,
            //as it forcefully causes growth to start without increasing severity.
            if (args.Severity >= 1)
                _signal.InvokePort(ent, component.SupercritPort);
        }
    }

    private void OnAnomalyStabilityChanged(ref AnomalyStabilityChangedEvent args)
    {
        Entity<AnomalyComponent> anomaly = (args.Anomaly, Comp<AnomalyComponent>(args.Anomaly));

        var query = EntityQueryEnumerator<AnomalySynchronizerComponent>();
        while (query.MoveNext(out var ent, out var component))
        {
            if (component.ConnectedAnomaly != anomaly)
                continue;

            if (!_power.IsPowered(ent))
                continue;

            if (args.Stability < anomaly.Comp.DecayThreshold)
            {
                _signal.InvokePort(ent, component.DecayingPort);
            }
            else if (args.Stability > anomaly.Comp.GrowthThreshold)
            {
                _signal.InvokePort(ent, component.GrowingPort);
            }
            else
            {
                _signal.InvokePort(ent, component.StabilizePort);
            }
        }
    }
}

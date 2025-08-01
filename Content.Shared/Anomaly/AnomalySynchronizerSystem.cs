using System.Linq;
using Content.Shared.Anomaly.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Anomaly;

/// <summary>
/// A device that allows you to translate anomaly activity into multitool signals.
/// </summary>
public sealed partial class AnomalySynchronizerSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAnomalySystem _anomaly = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<AnomalySynchronizerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var sync, out var synchronizerTransform))
        {
            if (sync.ConnectedAnomaly == null)
                continue;

            if (curTime < sync.NextCheckTime)
                continue;

            sync.NextCheckTime += sync.CheckFrequency;
            Dirty(uid, sync);

            if (TerminatingOrDeleted(sync.ConnectedAnomaly))
            {
                DisconnectFromAnomaly((uid, sync));
                continue;
            }

            // Use TryComp instead of Transform(uid) to take care of cases where the anomaly is out of
            // PVS range on the client, but the synchronizer isn't.
            if (!TryComp(sync.ConnectedAnomaly.Value, out TransformComponent? anomalyTransform))
                continue;

            if (anomalyTransform.MapUid != synchronizerTransform.MapUid)
            {
                DisconnectFromAnomaly((uid, sync));
                continue;
            }

            if (!synchronizerTransform.Coordinates.TryDistance(EntityManager, anomalyTransform.Coordinates, out var distance))
                continue;

            if (distance > sync.AttachRange)
                DisconnectFromAnomaly((uid, sync));
        }
    }

    /// <summary>
    /// If powered, try to attach a nearby anomaly.
    /// </summary>
    public bool TryAttachNearbyAnomaly(Entity<AnomalySynchronizerComponent> ent, EntityUid? user = null)
    {
        if (!_power.IsPowered(ent.Owner))
        {
            _popup.PopupClient(Loc.GetString("base-computer-ui-component-not-powered", ("machine", ent)), ent, user);
            return false;
        }

        var coords = _transform.GetMapCoordinates(ent);
        var anomaly = _entityLookup.GetEntitiesInRange<AnomalyComponent>(coords, ent.Comp.AttachRange).FirstOrDefault();

        if (anomaly.Owner is { Valid: false }) // no anomaly in range
        {
            _popup.PopupClient(Loc.GetString("anomaly-sync-no-anomaly"), ent, user);
            return false;
        }

        ConnectToAnomaly(ent, anomaly, user);
        return true;
    }

    private void OnPowerChanged(Entity<AnomalySynchronizerComponent> ent, ref PowerChangedEvent args)
    {
        if (args.Powered)
            return;

        if (ent.Comp.ConnectedAnomaly == null)
            return;

        DisconnectFromAnomaly(ent);
    }

    private void OnExamined(Entity<AnomalySynchronizerComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(ent.Comp.ConnectedAnomaly.HasValue ? "anomaly-sync-examine-connected" : "anomaly-sync-examine-not-connected"));
    }

    private void OnGetInteractionVerbs(Entity<AnomalySynchronizerComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var user = args.User;

        if (ent.Comp.ConnectedAnomaly == null)
        {
            args.Verbs.Add(new()
            {
                Act = () => TryAttachNearbyAnomaly(ent, user),
                Message = Loc.GetString("anomaly-sync-connect-verb-message", ("machine", ent)),
                Text = Loc.GetString("anomaly-sync-connect-verb-text"),
            });
        }
        else
        {
            args.Verbs.Add(new()
            {
                Act = () => DisconnectFromAnomaly(ent, user),
                Message = Loc.GetString("anomaly-sync-disconnect-verb-message", ("machine", ent)),
                Text = Loc.GetString("anomaly-sync-disconnect-verb-text"),
            });
        }
    }

    private void OnInteractHand(Entity<AnomalySynchronizerComponent> ent, ref InteractHandEvent args)
    {
        TryAttachNearbyAnomaly(ent, args.User);
    }

    private void ConnectToAnomaly(Entity<AnomalySynchronizerComponent> ent, Entity<AnomalyComponent> anomaly, EntityUid? user = null)
    {
        if (ent.Comp.ConnectedAnomaly == anomaly)
            return;

        ent.Comp.ConnectedAnomaly = anomaly;
        Dirty(ent);
        //move the anomaly to the center of the synchronizer, for aesthetics.
        var targetXform = _transform.GetWorldPosition(ent);
        _transform.SetWorldPosition(anomaly, targetXform);

        if (ent.Comp.PulseOnConnect)
            _anomaly.DoAnomalyPulse(anomaly, anomaly);

        _popup.PopupPredicted(Loc.GetString("anomaly-sync-connected"), ent, user, PopupType.Medium);
        _audio.PlayPredicted(ent.Comp.ConnectedSound, ent, user);
    }

    //TODO: disconnection from the anomaly should also be triggered if the anomaly is far away from the synchronizer.
    //Currently only bluespace anomaly can do this, but for some reason it is the only one that cannot be connected to the synchronizer.
    private void DisconnectFromAnomaly(Entity<AnomalySynchronizerComponent> ent, EntityUid? user = null)
    {
        if (ent.Comp.ConnectedAnomaly == null)
            return;

        if (ent.Comp.PulseOnDisconnect && TryComp<AnomalyComponent>(ent.Comp.ConnectedAnomaly, out var anomaly))
        {
            _anomaly.DoAnomalyPulse(ent.Comp.ConnectedAnomaly.Value, anomaly);
        }

        _popup.PopupPredicted(Loc.GetString("anomaly-sync-disconnected"), ent, user, PopupType.Large);
        _audio.PlayPredicted(ent.Comp.DisconnectedSound, ent, user);

        ent.Comp.ConnectedAnomaly = null;
        Dirty(ent);
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

            _deviceLink.InvokePort(uid, component.PulsePort);
        }
    }

    private void OnAnomalySeverityChanged(ref AnomalySeverityChangedEvent args)
    {
        var query = EntityQueryEnumerator<AnomalySynchronizerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (args.Anomaly != component.ConnectedAnomaly)
                continue;

            if (!_power.IsPowered(uid))
                continue;

            //The superscritical port is invoked not at the AnomalySupercriticalEvent,
            //but at the moment the growth animation starts. Otherwise, there is no point in this port.
            //ATTENTION! the console command supercriticalanomaly does not work here,
            //as it forcefully causes growth to start without increasing severity.
            if (args.Severity >= 1)
                _deviceLink.InvokePort(uid, component.SupercritPort);
        }
    }

    private void OnAnomalyStabilityChanged(ref AnomalyStabilityChangedEvent args)
    {
        var anomaly = Comp<AnomalyComponent>(args.Anomaly);

        var query = EntityQueryEnumerator<AnomalySynchronizerComponent>();
        while (query.MoveNext(out var uid, out var sync))
        {
            if (sync.ConnectedAnomaly != args.Anomaly)
                continue;

            if (!_power.IsPowered(uid))
                continue;

            if (args.Stability < anomaly.DecayThreshold)
            {
                _deviceLink.InvokePort(uid, sync.DecayingPort);
            }
            else if (args.Stability > anomaly.GrowthThreshold)
            {
                _deviceLink.InvokePort(uid, sync.GrowingPort);
            }
            else
            {
                _deviceLink.InvokePort(uid, sync.StabilizePort);
            }
        }
    }
}

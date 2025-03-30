using Content.Server.Administration.Logs;
using Content.Server.Body.Systems;
using Content.Server.Chat.Managers;
using Content.Server.Jittering;
using Content.Server.Mind;
using Content.Server.Stunnable;
using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects;
using Content.Shared.Body.Components;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;

namespace Content.Server.Anomaly.Effects;

public sealed class InnerBodyAnomalySystem : SharedInnerBodyAnomalySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly AnomalySystem _anomaly = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly JitteringSystem _jitter = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly StunSystem _stun = default!;

    private readonly Color _messageColor = Color.FromSrgb(new Color(201, 22, 94));

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InnerBodyAnomalyInjectorComponent, StartCollideEvent>(OnStartCollideInjector);

        SubscribeLocalEvent<InnerBodyAnomalyComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<InnerBodyAnomalyComponent, ComponentShutdown>(OnCompShutdown);

        SubscribeLocalEvent<InnerBodyAnomalyComponent, AnomalyPulseEvent>(OnAnomalyPulse);
        SubscribeLocalEvent<InnerBodyAnomalyComponent, AnomalyShutdownEvent>(OnAnomalyShutdown);
        SubscribeLocalEvent<InnerBodyAnomalyComponent, AnomalySupercriticalEvent>(OnAnomalySupercritical);
        SubscribeLocalEvent<InnerBodyAnomalyComponent, AnomalySeverityChangedEvent>(OnSeverityChanged);

        SubscribeLocalEvent<InnerBodyAnomalyComponent, MobStateChangedEvent>(OnMobStateChanged);

        SubscribeLocalEvent<AnomalyComponent, ActionAnomalyPulseEvent>(OnActionPulse);
    }

    private void OnActionPulse(Entity<AnomalyComponent> ent, ref ActionAnomalyPulseEvent args)
    {
        if (args.Handled)
            return;

        _anomaly.DoAnomalyPulse(ent, ent.Comp);

        args.Handled = true;
    }

    private void OnStartCollideInjector(Entity<InnerBodyAnomalyInjectorComponent> ent, ref StartCollideEvent args)
    {
        if (ent.Comp.Whitelist is not null && !_whitelist.IsValid(ent.Comp.Whitelist, args.OtherEntity))
            return;
        if (TryComp<InnerBodyAnomalyComponent>(args.OtherEntity, out var innerAnom) && innerAnom.Injected)
            return;
        if (!_mind.TryGetMind(args.OtherEntity, out _, out var mindComponent))
            return;

        EntityManager.AddComponents(args.OtherEntity, ent.Comp.InjectionComponents);
        QueueDel(ent);
    }

    private void OnMapInit(Entity<InnerBodyAnomalyComponent> ent, ref MapInitEvent args)
    {
        AddAnomalyToBody(ent);
    }

    private void AddAnomalyToBody(Entity<InnerBodyAnomalyComponent> ent)
    {
        if (!_proto.TryIndex(ent.Comp.InjectionProto, out var injectedAnom))
            return;

        if (ent.Comp.Injected)
            return;

        ent.Comp.Injected = true;

        EntityManager.AddComponents(ent, injectedAnom.Components);

        _stun.TryParalyze(ent, TimeSpan.FromSeconds(ent.Comp.StunDuration), true);
        _jitter.DoJitter(ent, TimeSpan.FromSeconds(ent.Comp.StunDuration), true);

        if (ent.Comp.StartSound is not null)
            _audio.PlayPvs(ent.Comp.StartSound, ent);

        if (ent.Comp.StartMessage is not null &&
            _mind.TryGetMind(ent, out _, out var mindComponent) &&
            mindComponent.Session != null)
        {
            var message = Loc.GetString(ent.Comp.StartMessage);
            var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
            _chat.ChatMessageToOne(ChatChannel.Server,
                message,
                wrappedMessage,
                default,
                false,
                mindComponent.Session.Channel,
                _messageColor);

            _popup.PopupEntity(message, ent, ent, PopupType.MediumCaution);

            _adminLog.Add(LogType.Anomaly,LogImpact.Medium,$"{ToPrettyString(ent)} became anomaly host.");
        }
        Dirty(ent);
    }

    private void OnAnomalyPulse(Entity<InnerBodyAnomalyComponent> ent, ref AnomalyPulseEvent args)
    {
        _stun.TryParalyze(ent, TimeSpan.FromSeconds(ent.Comp.StunDuration / 2 * args.Severity), true);
        _jitter.DoJitter(ent, TimeSpan.FromSeconds(ent.Comp.StunDuration / 2 * args.Severity), true);
    }

    private void OnAnomalySupercritical(Entity<InnerBodyAnomalyComponent> ent, ref AnomalySupercriticalEvent args)
    {
        if (!TryComp<BodyComponent>(ent, out var body))
            return;

        _body.GibBody(ent, true, body, splatModifier: 5f);
    }

    private void OnSeverityChanged(Entity<InnerBodyAnomalyComponent> ent, ref AnomalySeverityChangedEvent args)
    {
        if (!_mind.TryGetMind(ent, out _, out var mindComponent) || mindComponent.Session == null)
            return;

        var message = string.Empty;

        if (args.Severity >= 0.5 && ent.Comp.LastSeverityInformed < 0.5)
        {
            ent.Comp.LastSeverityInformed = 0.5f;
            message = Loc.GetString("inner-anomaly-severity-info-50");
        }
        if (args.Severity >= 0.75 && ent.Comp.LastSeverityInformed < 0.75)
        {
            ent.Comp.LastSeverityInformed = 0.75f;
            message = Loc.GetString("inner-anomaly-severity-info-75");
        }
        if (args.Severity >= 0.9 && ent.Comp.LastSeverityInformed < 0.9)
        {
            ent.Comp.LastSeverityInformed = 0.9f;
            message = Loc.GetString("inner-anomaly-severity-info-90");
        }
        if (args.Severity >= 1 && ent.Comp.LastSeverityInformed < 1)
        {
            ent.Comp.LastSeverityInformed = 1f;
            message = Loc.GetString("inner-anomaly-severity-info-100");
        }

        if (message == string.Empty)
            return;

        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
        _chat.ChatMessageToOne(ChatChannel.Server,
            message,
            wrappedMessage,
            default,
            false,
            mindComponent.Session.Channel,
            _messageColor);

        _popup.PopupEntity(message, ent, ent, PopupType.MediumCaution);
    }

    private void OnMobStateChanged(Entity<InnerBodyAnomalyComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        var ev = new BeforeRemoveAnomalyOnDeathEvent();
        RaiseLocalEvent(args.Target, ref ev);
        if (ev.Cancelled)
            return;

        _anomaly.ChangeAnomalyHealth(ent, -2); //Shutdown it
    }

    private void OnAnomalyShutdown(Entity<InnerBodyAnomalyComponent> ent, ref AnomalyShutdownEvent args)
    {
        RemoveAnomalyFromBody(ent);
        RemCompDeferred<InnerBodyAnomalyComponent>(ent);
    }

    private void OnCompShutdown(Entity<InnerBodyAnomalyComponent> ent, ref ComponentShutdown args)
    {
        RemoveAnomalyFromBody(ent);
    }

    private void RemoveAnomalyFromBody(Entity<InnerBodyAnomalyComponent> ent)
    {
        if (!ent.Comp.Injected)
            return;

        if (_proto.TryIndex(ent.Comp.InjectionProto, out var injectedAnom))
            EntityManager.RemoveComponents(ent, injectedAnom.Components);

        _stun.TryParalyze(ent, TimeSpan.FromSeconds(ent.Comp.StunDuration), true);

        if (ent.Comp.EndMessage is not null &&
            _mind.TryGetMind(ent, out _, out var mindComponent) &&
            mindComponent.Session != null)
        {
            var message = Loc.GetString(ent.Comp.EndMessage);
            var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
            _chat.ChatMessageToOne(ChatChannel.Server,
                message,
                wrappedMessage,
                default,
                false,
                mindComponent.Session.Channel,
                _messageColor);


            _popup.PopupEntity(message, ent, ent, PopupType.MediumCaution);

            _adminLog.Add(LogType.Anomaly, LogImpact.Medium,$"{ToPrettyString(ent)} is no longer a host for the anomaly.");
        }

        ent.Comp.Injected = false;
        RemCompDeferred<AnomalyComponent>(ent);
    }
}

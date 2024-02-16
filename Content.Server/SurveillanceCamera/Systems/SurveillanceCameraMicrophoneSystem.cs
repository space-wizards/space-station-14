using Content.Server.Chat.V2;
using Content.Server.Speech;
using Content.Server.Speech.Components;
using Content.Shared.Chat.V2;
using Content.Shared.Interaction;
using Robust.Shared.Player;

namespace Content.Server.SurveillanceCamera;

public sealed class SurveillanceCameraMicrophoneSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xforms = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SurveillanceCameraMicrophoneComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SurveillanceCameraMicrophoneComponent, ListenEvent>(RelayEntityMessage);
        SubscribeLocalEvent<SurveillanceCameraMicrophoneComponent, ListenAttemptEvent>(CanListen);

        SubscribeLocalEvent<LocalChatSuccessEvent>(DuplicateLocalChatEventsIfInRange);
        SubscribeLocalEvent<EntityWhisperedLocalEvent>(DuplicateWhisperEventsIfInRange);
    }

    private void DuplicateLocalChatEventsIfInRange(LocalChatSuccessEvent ev)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourceXform = Transform(GetEntity(ev.Speaker));
        var sourcePos = _xforms.GetWorldPosition(sourceXform, xformQuery);

        foreach (var (_, __, camera, xform) in EntityQuery<SurveillanceCameraMicrophoneComponent, ActiveListenerComponent, SurveillanceCameraComponent, TransformComponent>())
        {
            if (camera.ActiveViewers.Count == 0)
                continue;

            var range = (xform.MapID != sourceXform.MapID)
                ? -1
                : (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).Length();

            if (range < 0 || range > ev.Range)
                continue;

            foreach (var viewer in camera.ActiveViewers)
            {
                if (TryComp(viewer, out ActorComponent? actor))
                {
                    RaiseNetworkEvent(ev, actor.PlayerSession);
                }
            }
        }
    }

    private void DuplicateWhisperEventsIfInRange(EntityWhisperedLocalEvent ev)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourceXform = Transform(GetEntity(ev.Speaker));
        var sourcePos = _xforms.GetWorldPosition(sourceXform, xformQuery);

        foreach (var (_, __, camera, xform) in EntityQuery<SurveillanceCameraMicrophoneComponent, ActiveListenerComponent, SurveillanceCameraComponent, TransformComponent>())
        {
            if (camera.ActiveViewers.Count == 0)
                continue;

            var range = (xform.MapID != sourceXform.MapID)
                ? -1
                : (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).Length();

            EntityEventArgs outMsg;

            if (range < 0 || range > ev.MaxRange)
                // Not in range
                continue;

            if (range < ev.MinRange)
            {
                outMsg = new EntityWhisperedEvent(ev.Speaker, ev.AsName, ev.FontId, ev.FontSize, ev.IsBold, ev.AsColor,
                    ev.MinRange, ev.Message);
            }
            else if (_interactionSystem.InRangeUnobstructed(_xforms.GetMapCoordinates(xform), GetEntity(ev.Speaker), ev.MaxRange, Shared.Physics.CollisionGroup.Opaque))
            {
                outMsg = new EntityWhisperedObfuscatedlyEvent(ev.Speaker, ev.AsName, ev.FontId, ev.FontSize, ev.IsBold, ev.AsColor,
                    ev.MaxRange, ev.ObfuscatedMessage);
            }
            else
            {
                outMsg = new EntityWhisperedTotallyObfuscatedlyEvent(ev.Speaker, ev.FontId, ev.FontSize, ev.IsBold,
                    ev.MaxRange, ev.ObfuscatedMessage);
            }

            foreach (var viewer in camera.ActiveViewers)
            {
                if (TryComp(viewer, out ActorComponent? actor))
                {
                    RaiseNetworkEvent(outMsg, actor.PlayerSession);
                }
            }
        }
    }

    private void OnInit(EntityUid uid, SurveillanceCameraMicrophoneComponent component, ComponentInit args)
    {
        if (component.Enabled)
            EnsureComp<ActiveListenerComponent>(uid).Range = component.Range;
        else
            RemCompDeferred<ActiveListenerComponent>(uid);
    }

    public void CanListen(EntityUid uid, SurveillanceCameraMicrophoneComponent microphone, ListenAttemptEvent args)
    {
        // TODO maybe just make this a part of ActiveListenerComponent?
        if (microphone.Blacklist.IsValid(args.Source))
            args.Cancel();
    }

    public void RelayEntityMessage(EntityUid uid, SurveillanceCameraMicrophoneComponent component, ListenEvent args)
    {
        if (!TryComp(uid, out SurveillanceCameraComponent? camera))
            return;

        var ev = new SurveillanceCameraSpeechSendEvent(args.Source, args.Message);

        foreach (var monitor in camera.ActiveMonitors)
        {
            RaiseLocalEvent(monitor, ev);
        }
    }
}

public sealed class SurveillanceCameraSpeechSendEvent : EntityEventArgs
{
    public EntityUid Speaker { get; }
    public string Message { get; }

    public SurveillanceCameraSpeechSendEvent(EntityUid speaker, string message)
    {
        Speaker = speaker;
        Message = message;
    }
}


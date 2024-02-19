using Content.Server.Chat.V2;
using Content.Server.Speech.Components;
using Content.Shared.Chat.V2;

namespace Content.Server.Speech.EntitySystems;

/// <summary>
///     This system redirects local chat messages to listening entities (e.g., radio microphones).
/// </summary>
public sealed class ListeningSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xforms = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LocalChatCreatedEvent>(OnSpeak);
        SubscribeLocalEvent<WhisperCreatedEvent>(OnWhisper);
    }

    private void OnSpeak(LocalChatCreatedEvent ev)
    {
        PingListeners(ev.Speaker, ev.Message, "");
    }

    private void OnWhisper(WhisperCreatedEvent ev)
    {
        PingListeners(ev.Speaker, ev.Message, ev.ObfuscatedMessage, ev.MinRange);
    }

    public void PingListeners(EntityUid source, string message, string? obfuscatedMessage = null, float whisperRange = 0.0f)
    {
        // TODO whispering / audio volume? Microphone sensitivity?
        // for now, whispering just arbitrarily reduces the listener's max range.

        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourceXform = xformQuery.GetComponent(source);
        var sourcePos = _xforms.GetWorldPosition(sourceXform, xformQuery);

        var attemptEv = new ListenAttemptEvent(source);
        var ev = new ListenEvent(message, source);
        var obfuscatedEv = !string.IsNullOrEmpty(obfuscatedMessage) ? new ListenEvent(obfuscatedMessage, source) : null;
        var query = EntityQueryEnumerator<ActiveListenerComponent, TransformComponent>();

        while(query.MoveNext(out var listenerUid, out var listener, out var xform))
        {
            if (xform.MapID != sourceXform.MapID)
                continue;

            // range checks
            // TODO proper speech occlusion
            var distance = (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).LengthSquared();
            if (distance > listener.Range * listener.Range)
                continue;

            RaiseLocalEvent(listenerUid, attemptEv);
            if (attemptEv.Cancelled)
            {
                attemptEv.Uncancel();
                continue;
            }

            if (obfuscatedEv != null && distance > whisperRange)
                RaiseLocalEvent(listenerUid, obfuscatedEv);
            else
                RaiseLocalEvent(listenerUid, ev);
        }
    }
}

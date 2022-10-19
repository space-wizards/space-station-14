using Content.Server.Chat.Systems;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

/// <summary>
///     This system redirects local chat messages to listening entities (e.g., radio microphones).
/// </summary>
public sealed class ListeningSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xforms = default!;

    private const float WhisperMultiplier = ChatSystem.WhisperRange / ChatSystem.VoiceRange;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EntitySpokeEvent>(OnSpeak);
    }

    private void OnSpeak(EntitySpokeEvent ev)
    {
        PingListeners(ev.Source, ev.Message, ev.Whisper);
    }

    public void PingListeners(EntityUid source, string message, bool whispering)
    {
        // TODO whispering / audio volume? Microphone sensitivity?
        // for now, whispering just arbitrarily reduces the listener's max range.

        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourceXform = xformQuery.GetComponent(source);
        var sourcePos = _xforms.GetWorldPosition(sourceXform, xformQuery);

        var attemptEv = new ListenAttemptEvent(message, source);
        var ev = new ListenEvent(message, source);

        foreach (var (listener, xform) in EntityQuery<ActiveListenerComponent, TransformComponent>())
        {
            if (xform.MapID != sourceXform.MapID)
                return;

            // TODO proper speech occlusion

            // This is very arbitrary
            var effectiveRange = whispering ? listener.Range * WhisperMultiplier : listener.Range;

            // range checks
            var pos = _xforms.GetWorldPosition(xform, xformQuery);
            if ((pos - sourcePos).LengthSquared > effectiveRange * effectiveRange)
                continue;

            RaiseLocalEvent(listener.Owner, attemptEv);
            if (attemptEv.Cancelled)
            {
                attemptEv.Uncancel();
                continue;
            }

            RaiseLocalEvent(listener.Owner, ev);
        }
    }
}

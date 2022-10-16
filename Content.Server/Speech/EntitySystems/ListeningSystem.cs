using Content.Server.Chat.Systems;
using Content.Server.Speech.Components;
using Content.Shared.Interaction;
using Robust.Shared.Map;

namespace Content.Server.Speech.EntitySystems;

/// <summary>
///     This system redirects local chat messages to listening entities (e.g., radio microphones).
/// </summary>
public sealed class ListeningSystem : EntitySystem
{
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;

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
        MapCoordinates sourcePos = new(_xforms.GetWorldPosition(sourceXform, xformQuery), sourceXform.MapID);

        var attemptEv = new ListenAttemptEvent(message, source);
        var ev = new ListenEvent(message, source);

        foreach (var (listener, xform) in EntityQuery<ActiveListenerComponent, TransformComponent>())
        {
            if (xform.MapID != sourceXform.MapID)
                return;

            // this is very arbitrary
            var effectiveRange = whispering ? listener.Range / 3 : listener.Range;

            // range checks
            MapCoordinates pos = new(_xforms.GetWorldPosition(xform, xformQuery), xform.MapID);
            if (listener.RequireUnobstructed)
            {
                if (!_interaction.InRangeUnobstructed(sourcePos, pos, range: effectiveRange))
                    continue;
            }
            else if ((pos.Position - sourcePos.Position).LengthSquared > effectiveRange * effectiveRange)
                continue;

            // attempt event.
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
